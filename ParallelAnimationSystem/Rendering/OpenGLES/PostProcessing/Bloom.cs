using OpenTK.Graphics.OpenGLES2;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

public class Bloom(IResourceManager resourceManager) : IDisposable
{
    private int prefilterProgram, blurProgram, upsampleProgram, combineProgram;
    private int blurSizeUniformLocation, blurVerticalUniformLocation;
    private int upsampleSizeUniformLocation, upsampleDiffusionUniformLocation, upsampleLowMipUniformLocation, upsampleHighMipUniformLocation;
    private int combineIntensityUniformLocation, combineTexture1UniformLocation, combineTexture2UniformLocation;
    
    private int textureSampler;
    private int framebuffer;
    
    private readonly List<int> downsamplingMipChain = [];
    private readonly List<int> downsamplingMipChainTemp = [];
    private readonly List<int> upsamplingMipChain = [];
    private Vector2i currentSize = -Vector2i.One;

    public void Initialize(int vertexShader)
    {
        prefilterProgram = CreateProgram("BloomPrefilter", vertexShader);
        blurProgram = CreateProgram("BloomBlur", vertexShader);
        upsampleProgram = CreateProgram("BloomUpsample", vertexShader);
        combineProgram = CreateProgram("BloomCombine", vertexShader);
        
        blurSizeUniformLocation = GL.GetUniformLocation(blurProgram, "uSize");
        blurVerticalUniformLocation = GL.GetUniformLocation(blurProgram, "uVertical");
        
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
        if (intensity == 0.0f || diffusion == 0.0f)
            return false;
        
        // Update mip chain if size has changed
        if (size != currentSize)
        {
            UpdateTextures(size);
            currentSize = size;
        }
        
        // Prefilter to mip 0
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, downsamplingMipChain[0], 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        GL.UseProgram(prefilterProgram);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        // Downsample
        GL.UseProgram(blurProgram);
        
        for (var i = 1; i < downsamplingMipChain.Count; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            // Blur downsample[i - 1] to downsampleTemp[i - 1]
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, downsamplingMipChainTemp[i - 1], 0);
            GL.Viewport(0, 0, mipSize.X, mipSize.Y);
            
            GL.Uniform2i(blurSizeUniformLocation, mipSize.X, mipSize.Y);
            GL.Uniform1i(blurVerticalUniformLocation, 0);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, downsamplingMipChain[i - 1]);
            GL.BindSampler(0, textureSampler);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            
            // Blur downsampleTemp[i - 1] to downsample[i]
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, downsamplingMipChain[i], 0);
            
            GL.Uniform1i(blurVerticalUniformLocation, 1);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, downsamplingMipChainTemp[i - 1]);
            GL.BindSampler(0, textureSampler);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Upsample
        GL.UseProgram(upsampleProgram);
        
        for (var i = 0; i < upsamplingMipChain.Count; i++)
        {
            var lowMip = i == 0 ? downsamplingMipChain[^1] : upsamplingMipChain[i - 1];
            var highMip = downsamplingMipChain[^(i + 1)];
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
            GL.BindSampler(0, textureSampler);
            
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, highMip);
            GL.BindSampler(1, textureSampler);
            
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
        GL.BindSampler(0, textureSampler);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, upsamplingMipChain[^1]);
        GL.BindSampler(1, textureSampler);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    private void UpdateTextures(Vector2i size)
    {
        // Delete old mip chain
        foreach (var mip in downsamplingMipChain)
            GL.DeleteTexture(mip);
        
        foreach (var mip in downsamplingMipChainTemp)
            GL.DeleteTexture(mip);
        
        foreach (var mip in upsamplingMipChain)
            GL.DeleteTexture(mip);
        
        downsamplingMipChain.Clear();
        downsamplingMipChainTemp.Clear();
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
        
        // Initialize downsampling mip chain temp
        for (var i = 1; i < mipLevels; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            var mip = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, mip);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            downsamplingMipChainTemp.Add(mip);
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
    
    private int CreateProgram(string shaderName, int vertexShader)
    {
        var shaderSource = resourceManager.LoadGraphicsResourceString($"Shaders/PostProcessing/{shaderName}.glsl");
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, shaderSource);
        GL.CompileShader(fragmentShader);
        
        var fragmentShaderCompileStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (fragmentShaderCompileStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new Exception($"Failed to compile fragment shader: {infoLog}");
        }
        
        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        var programLinkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (programLinkStatus == 0)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new Exception($"Failed to link program: {infoLog}");
        }
        
        GL.DeleteShader(fragmentShader);
        
        return program;
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    public void Dispose()
    {
        GL.DeleteProgram(prefilterProgram);
        GL.DeleteProgram(blurProgram);
        GL.DeleteProgram(upsampleProgram);
        GL.DeleteProgram(combineProgram);
        
        GL.DeleteSampler(textureSampler);
        GL.DeleteFramebuffer(framebuffer);
        
        foreach (var mip in downsamplingMipChain)
            GL.DeleteTexture(mip);
        
        foreach (var mip in downsamplingMipChainTemp)
            GL.DeleteTexture(mip);
        
        foreach (var mip in upsamplingMipChain)
            GL.DeleteTexture(mip);
    }
}