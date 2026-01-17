using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

public class Bloom : IDisposable
{
    private readonly int prefilterProgram, downsampleProgram, upsampleProgram, combineProgram;
    private readonly int prefilterThresholdUniformLocation;
    private readonly int downsampleSizeUniformLocation;
    private readonly int upsampleSizeUniformLocation, upsampleDiffusionUniformLocation, upsampleLowMipUniformLocation, upsampleHighMipUniformLocation;
    private readonly int combineIntensityUniformLocation, combineTexture1UniformLocation, combineTexture2UniformLocation;
    
    private readonly int textureSampler;
    private readonly int framebuffer;
    
    private readonly List<int> downsamplingMipChain = [];
    private readonly List<int> upsamplingMipChain = [];
    private Vector2i currentSize = -Vector2i.One;

    public Bloom(ResourceLoader loader, int vertexShader)
    {
        prefilterProgram = LoaderUtil.LoadPPProgram(loader, "BloomPrefilter", vertexShader);
        downsampleProgram = LoaderUtil.LoadPPProgram(loader, "BloomDownsample", vertexShader);
        upsampleProgram = LoaderUtil.LoadPPProgram(loader, "BloomUpsample", vertexShader);
        combineProgram = LoaderUtil.LoadPPProgram(loader, "BloomCombine", vertexShader);
        
        prefilterThresholdUniformLocation = GL.GetUniformLocation(prefilterProgram, "uThreshold");
        
        downsampleSizeUniformLocation = GL.GetUniformLocation(downsampleProgram, "uSize");
        
        upsampleSizeUniformLocation = GL.GetUniformLocation(upsampleProgram, "uSize");
        upsampleDiffusionUniformLocation = GL.GetUniformLocation(upsampleProgram, "uDiffusion");
        upsampleLowMipUniformLocation = GL.GetUniformLocation(upsampleProgram, "uLowMip");
        upsampleHighMipUniformLocation = GL.GetUniformLocation(upsampleProgram, "uHighMip");
        
        combineIntensityUniformLocation = GL.GetUniformLocation(combineProgram, "uIntensity");
        combineTexture1UniformLocation = GL.GetUniformLocation(combineProgram, "uTexture1");
        combineTexture2UniformLocation = GL.GetUniformLocation(combineProgram, "uTexture2");
        
        textureSampler = GL.GenSampler();
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        framebuffer = GL.GenFramebuffer();
    }

    public bool Process(Vector2i size, float intensity, float diffusion, int inputTexture, int outputTexture)
    {
        if (intensity == 0.0f)
            return false;
        
        // diffusion = MathHelper.Lerp(0.05f, 0.95f, diffusion);
        diffusion = MathUtil.Lerp(0.5f, 0.95f, diffusion);
        
        // Update mip chain if size has changed
        if (size != currentSize)
        {
            UpdateTextures(size);
            currentSize = size;
        }
        
        // Bind samplers
        GL.BindSampler(0, textureSampler);
        GL.BindSampler(1, textureSampler);
        
        // Prefilter to mip 0
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, downsamplingMipChain[0], 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        GL.UseProgram(prefilterProgram);
        
        GL.Uniform1f(prefilterThresholdUniformLocation, 0.95f);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        // Downsample
        GL.UseProgram(downsampleProgram);
        
        for (var i = 1; i < downsamplingMipChain.Count; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            // Blur downsample[i - 1] to downsample[i]
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, downsamplingMipChain[i], 0);
            GL.Viewport(0, 0, mipSize.X, mipSize.Y);
            
            GL.Uniform2i(downsampleSizeUniformLocation, mipSize.X, mipSize.Y);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, downsamplingMipChain[i - 1]);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Upsample
        GL.UseProgram(upsampleProgram);
        
        for (var i = 0; i < upsamplingMipChain.Count; i++)
        {
            var lowMip = i == 0 ? downsamplingMipChain[^1] : upsamplingMipChain[i - 1];
            var highMip = downsamplingMipChain[^(i + 2)];
            var outputMip = upsamplingMipChain[i];
            var outputSize = new Vector2i(size.X >> (upsamplingMipChain.Count - i - 1), size.Y >> (upsamplingMipChain.Count - i - 1));
            
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, outputMip, 0);
            GL.Viewport(0, 0, outputSize.X, outputSize.Y);
            
            GL.Uniform2i(upsampleSizeUniformLocation, outputSize.X, outputSize.Y);
            GL.Uniform1f(upsampleDiffusionUniformLocation, diffusion);
            GL.Uniform1i(upsampleLowMipUniformLocation, 0);
            GL.Uniform1i(upsampleHighMipUniformLocation, 1);
            
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
        
        GL.Uniform1f(combineIntensityUniformLocation, intensity);
        GL.Uniform1i(combineTexture1UniformLocation, 0);
        GL.Uniform1i(combineTexture2UniformLocation, 1);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, upsamplingMipChain[^1]);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    private void UpdateTextures(Vector2i size)
    {
        // Delete old mip chain
        foreach (var mip in downsamplingMipChain)
            GL.DeleteTexture(mip);
        
        foreach (var mip in upsamplingMipChain)
            GL.DeleteTexture(mip);
        
        downsamplingMipChain.Clear();
        upsamplingMipChain.Clear();
        
        // Create new mip chain
        var mipLevels = CalculateMipLevels(size);
        
        // Initialize downsampling mip chain
        for (var i = 0; i < mipLevels; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            var mip = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, mip);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            downsamplingMipChain.Add(mip);
        }
        
        // Initialize upsampling mip chain
        for (var i = mipLevels - 2; i >= 0; i--)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            var mip = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, mip);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            upsamplingMipChain.Add(mip);
        }
    }
    
    private static int CalculateMipLevels(Vector2i size)
    {
        var minDim = Math.Min(size.X, size.Y);
        return (int) MathF.Floor(MathF.Log(minDim, 2));
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    public void Dispose()
    {
        GL.DeleteProgram(prefilterProgram);
        GL.DeleteProgram(downsampleProgram);
        GL.DeleteProgram(upsampleProgram);
        GL.DeleteProgram(combineProgram);
        
        GL.DeleteSampler(textureSampler);
        GL.DeleteFramebuffer(framebuffer);
        
        foreach (var mip in downsamplingMipChain)
            GL.DeleteTexture(mip);
        
        foreach (var mip in upsamplingMipChain)
            GL.DeleteTexture(mip);
    }
}