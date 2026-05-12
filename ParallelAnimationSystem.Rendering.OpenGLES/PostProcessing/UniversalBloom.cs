using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

public class UniversalBloom : IDisposable
{
    private record struct Mip(int Down, int Up, Vector2i Size);
    
    private readonly int prefilterProgram, blurProgram, upsampleProgram, combineProgram;
    private readonly int
        prefilterThresholdUniformLocation,
        prefilterKneeUniformLocation,
        blurIsVerticalUniformLocation,
        blurTexelSizeUniformLocation,
        upsampleScatterUniformLocation,
        combineTintUniformLocation;
    
    private readonly int textureSampler;
    private readonly int framebuffer;
    private readonly int vaoHandle;

    private readonly List<Mip> mipChain = [];
    private Vector2i currentSize = -Vector2i.One;

    public UniversalBloom(ResourceLoader loader, int vertexShader)
    {
        prefilterProgram = LoaderUtil.LoadPostProcessingProgram(loader, "Bloom/Prefilter", vertexShader);
        blurProgram = LoaderUtil.LoadPostProcessingProgram(loader, "Bloom/Universal/Blur", vertexShader);
        upsampleProgram = LoaderUtil.LoadPostProcessingProgram(loader, "Bloom/Universal/Upsample", vertexShader);
        combineProgram = LoaderUtil.LoadPostProcessingProgram(loader, "Bloom/Combine", vertexShader);
        
        prefilterThresholdUniformLocation = GL.GetUniformLocation(prefilterProgram, "uThreshold");
        prefilterKneeUniformLocation = GL.GetUniformLocation(prefilterProgram, "uKnee");
        blurIsVerticalUniformLocation = GL.GetUniformLocation(blurProgram, "uIsVertical");
        blurTexelSizeUniformLocation = GL.GetUniformLocation(blurProgram, "uTexelSize");
        upsampleScatterUniformLocation = GL.GetUniformLocation(upsampleProgram, "uScatter");
        combineTintUniformLocation = GL.GetUniformLocation(combineProgram, "uTint");
        
        var upsampleLowMipSamplerUniformLocation = GL.GetUniformLocation(upsampleProgram, "uLowMipSampler");
        var upsampleHighMipSamplerUniformLocation = GL.GetUniformLocation(upsampleProgram, "uHighMipSampler");
        var combineSourceSamplerUniformLocation = GL.GetUniformLocation(combineProgram, "uSourceSampler");
        var combineBloomSamplerUniformLocation = GL.GetUniformLocation(combineProgram, "uBloomSampler");
        
        // Set sampler uniform binding
        GL.UseProgram(upsampleProgram);
        GL.Uniform1i(upsampleLowMipSamplerUniformLocation, 0);
        GL.Uniform1i(upsampleHighMipSamplerUniformLocation, 1);
        
        // Set sampler uniform binding
        GL.UseProgram(combineProgram);
        GL.Uniform1i(combineSourceSamplerUniformLocation, 0);
        GL.Uniform1i(combineBloomSamplerUniformLocation, 1);
        
        textureSampler = GL.GenSampler();
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        framebuffer = GL.GenFramebuffer();
        vaoHandle = GL.GenVertexArray();
    }

    public bool Process(Vector2i size, float intensity, float diffusion, ColorRgb color, int inputTexture, int outputTexture)
    {
        if (intensity == 0.0f)
            return false;
        
        // Update mip chain if size has changed
        if (size != currentSize)
        {
            currentSize = size;

            var s = MathF.Max(size.X, size.Y);
            var iterations = (int) MathF.Log2(s);
            UpdateMipChain(size, iterations);
        }
        
        if (mipChain.Count < 2)
            return false;
        
        // Get mip 0
        var mip0 = mipChain[0];
        
        // Bind vertex array
        GL.BindVertexArray(vaoHandle);
        
        // Bind samplers
        GL.BindSampler(0, textureSampler);
        GL.BindSampler(1, textureSampler);
        
        // Prefilter to mip 0
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, mip0.Down, 0); // Bind output texture
        GL.Viewport(0, 0, mip0.Size.X, mip0.Size.Y);
        
        GL.UseProgram(prefilterProgram);
        
        // Set knee and threshold uniforms
        var threshold = 0.81f; // TODO: expose as parameter
        var knee = 0.5f;
        
        GL.Uniform1f(prefilterThresholdUniformLocation, threshold);
        GL.Uniform1f(prefilterKneeUniformLocation, threshold * knee);
        
        // We use active texture 0
        GL.ActiveTexture(TextureUnit.Texture0);
        
        // Bind input texture
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        // Downsample
        GL.UseProgram(blurProgram);
        
        for (var i = 1; i < mipChain.Count; i++)
        {
            var sourceMip = mipChain[i - 1];
            var targetMip = mipChain[i];
            
            // Downsample pass (mip[i - 1] -> mip[i])
            GL.Uniform2f(blurTexelSizeUniformLocation, 1.0f / targetMip.Size.X, 1.0f / targetMip.Size.Y);
            
            // Horizontal pass
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, targetMip.Up, 0);
            GL.Viewport(0, 0, targetMip.Size.X, targetMip.Size.Y);
            
            GL.Uniform1i(blurIsVerticalUniformLocation, 0);
            
            GL.BindTexture(TextureTarget.Texture2d, sourceMip.Down);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            
            // Vertical pass
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, targetMip.Down, 0);
            
            GL.Uniform1i(blurIsVerticalUniformLocation, 1);
            
            GL.BindTexture(TextureTarget.Texture2d, targetMip.Up);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Upsample
        GL.UseProgram(upsampleProgram);
        
        // Set scatter uniform
        var scatter = MathUtil.MapRange(diffusion, 0.0f, 1.0f, 0.05f, 0.95f);
        GL.Uniform1f(upsampleScatterUniformLocation, scatter);
        
        for (var i = mipChain.Count - 2; i >= 0; i--)
        {
            var lowMip = i == mipChain.Count - 2 ? mipChain[i + 1].Down : mipChain[i + 1].Up;
            var highMip = mipChain[i].Down;
            var targetMip = mipChain[i]; // Up direction
            
            // Upsample pass (mip[i + 1] -> mip[i])
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, targetMip.Up, 0);
            GL.Viewport(0, 0, targetMip.Size.X, targetMip.Size.Y);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, lowMip);
            
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, highMip);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Combine
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, outputTexture, 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        GL.UseProgram(combineProgram);
        
        var colorLinear = new ColorRgb(color.R * color.R, color.G * color.G, color.B * color.B);
        var colorLuminance = 0.2126f * colorLinear.R + 0.7152f * colorLinear.G + 0.0722f * colorLinear.B;
        colorLinear = colorLuminance > 0f ? colorLinear * (1f / colorLuminance) : new ColorRgb(1f, 1f, 1f);
        var tint = colorLinear * intensity;
        GL.Uniform3f(combineTintUniformLocation, tint.R, tint.G, tint.B);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, mip0.Up);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    private void UpdateMipChain(Vector2i size, int levels)
    {
        // Delete old mip chain
        foreach (var mip in mipChain)
        {
            GL.DeleteTexture(mip.Down);
            GL.DeleteTexture(mip.Up);
        }
        
        mipChain.Clear();
        
        // Initialize mip chain
        for (var i = 0; i < levels; i++)
        {
            var mipSize = new Vector2i(
                Math.Max(size.X >> i, 1),
                Math.Max(size.Y >> i, 1));
            
            var down = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, down);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            
            var up = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, up);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            
            mipChain.Add(new Mip(down, up, mipSize));
            
            if (mipSize is { X: 1, Y: 1 })
                break; // Stop if we've reached 1x1
        }
    }

    public void Dispose()
    {
        GL.DeleteProgram(prefilterProgram);
        GL.DeleteProgram(blurProgram);
        GL.DeleteProgram(upsampleProgram);
        GL.DeleteProgram(combineProgram);
        
        GL.DeleteSampler(textureSampler);
        GL.DeleteFramebuffer(framebuffer);
        GL.DeleteVertexArray(vaoHandle);
        
        foreach (var mip in mipChain)
        {
            GL.DeleteTexture(mip.Down);
            GL.DeleteTexture(mip.Up);
        }
    }
}