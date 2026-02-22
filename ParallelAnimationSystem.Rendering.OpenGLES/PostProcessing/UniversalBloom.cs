using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

public class UniversalBloom : IDisposable
{
    private record struct Mip(int Handle, int Handle2, Vector2i Size);
    
    private readonly int prefilterProgram, blurProgram, upsampleProgram, combineProgram;
    private readonly int
        prefilterThresholdUniformLocation,
        prefilterCurveUniformLocation,
        blurIsVerticalUniformLocation,
        blurTexelSizeUniformLocation,
        upsampleScatterUniformLocation,
        combineIntensityUniformLocation;
    
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
        prefilterCurveUniformLocation = GL.GetUniformLocation(prefilterProgram, "uCurve");
        blurIsVerticalUniformLocation = GL.GetUniformLocation(blurProgram, "uIsVertical");
        blurTexelSizeUniformLocation = GL.GetUniformLocation(blurProgram, "uTexelSize");
        upsampleScatterUniformLocation = GL.GetUniformLocation(upsampleProgram, "uScatter");
        combineIntensityUniformLocation = GL.GetUniformLocation(combineProgram, "uIntensity");
        
        var combineSourceSamplerUniformLocation = GL.GetUniformLocation(combineProgram, "uSourceSampler");
        var combineBloomSamplerUniformLocation = GL.GetUniformLocation(combineProgram, "uBloomSampler");
        
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

    public bool Process(Vector2i size, float intensity, float diffusion, int inputTexture, int outputTexture)
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
        
        if (mipChain.Count == 0)
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
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, mip0.Handle, 0); // Bind output texture
        GL.Viewport(0, 0, size.X, size.Y);
        
        // Turn off blending
        GL.Disable(EnableCap.Blend);
        
        GL.UseProgram(prefilterProgram);
        
        // Set knee and threshold uniforms
        var threshold = 1.0f; // TODO: expose as parameter
        var softKnee = 0.5f;
        
        var knee = threshold * softKnee + 1e-5f;
        var curve0 = threshold - knee;
        var curve1 = knee * 2.0f;
        var curve2 = 0.25f / knee;
        
        GL.Uniform1f(prefilterThresholdUniformLocation, threshold);
        GL.Uniform3f(prefilterCurveUniformLocation, curve0, curve1, curve2);
        
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
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, targetMip.Handle2, 0);
            GL.Viewport(0, 0, targetMip.Size.X, targetMip.Size.Y);
            
            GL.Uniform1i(blurIsVerticalUniformLocation, 0);
            
            GL.BindTexture(TextureTarget.Texture2d, sourceMip.Handle);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            
            // Vertical pass
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, targetMip.Handle, 0);
            
            GL.Uniform1i(blurIsVerticalUniformLocation, 1);
            
            GL.BindTexture(TextureTarget.Texture2d, targetMip.Handle2);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Enable blending for upsample pass
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.Zero, BlendingFactor.One);
        
        // Upsample
        GL.UseProgram(upsampleProgram);
        
        // Set scatter uniform
        var scatter = MathUtil.MapRange(diffusion, 0.0f, 1.0f, 0.05f, 0.95f);
        GL.Uniform1f(upsampleScatterUniformLocation, scatter);
        
        for (var i = mipChain.Count - 2; i >= 0; i--)
        {
            var sourceMip = mipChain[i + 1];
            var targetMip = mipChain[i];
            
            // Upsample pass (mip[i + 1] -> mip[i])
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, targetMip.Handle, 0);
            GL.Viewport(0, 0, targetMip.Size.X, targetMip.Size.Y);
            
            GL.BindTexture(TextureTarget.Texture2d, sourceMip.Handle);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Disable blending
        GL.Disable(EnableCap.Blend);
        
        // Combine
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, outputTexture, 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        GL.UseProgram(combineProgram);
        
        GL.Uniform1f(combineIntensityUniformLocation, intensity);
        
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, mip0.Handle);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    private void UpdateMipChain(Vector2i size, int levels)
    {
        // Delete old mip chain
        foreach (var mip in mipChain)
        {
            GL.DeleteTexture(mip.Handle);
            if (mip.Handle2 != 0)
                GL.DeleteTexture(mip.Handle2);
        }
        
        mipChain.Clear();
        
        // Initialize mip chain
        for (var i = 0; i < levels; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            var mipHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, mipHandle);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            
            var mipHandle2 = 0;
            if (i != 0)
            {
                mipHandle2 = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2d, mipHandle2);
                GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            }
            
            mipChain.Add(new Mip(mipHandle, mipHandle2, mipSize));
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
            GL.DeleteTexture(mip.Handle);
            if (mip.Handle2 != 0)
                GL.DeleteTexture(mip.Handle2);
        }
    }
}