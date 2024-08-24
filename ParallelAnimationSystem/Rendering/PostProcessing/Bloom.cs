using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.PostProcessing;

public class Bloom : IDisposable
{
    private int prefilterProgram, downsampleProgram, blurProgram, upsampleProgram, combineProgram;
    private int 
        prefilterSizeUniformLocation,
        downsampleOutputSizeUniformLocation,
        blurSizeUniformLocation,
        blurVerticalUniformLocation,
        upsampleOutputSizeUniformLocation,
        upsampleDiffusionUniformLocation,
        combineSizeUniformLocation,
        combineIntensityUniformLocation;

    private int tempImage1, tempImage2;
    private int textureSampler;
    
    private readonly List<int> mipChain = [];
    private Vector2i currentSize = -Vector2i.One;

    public void Initialize()
    {
        prefilterProgram = CreateProgram("BloomPrefilter");
        downsampleProgram = CreateProgram("BloomDownsample");
        blurProgram = CreateProgram("BloomBlur");
        upsampleProgram = CreateProgram("BloomUpsample");
        combineProgram = CreateProgram("BloomCombine");
        
        prefilterSizeUniformLocation = GL.GetUniformLocation(prefilterProgram, "uSize");
        downsampleOutputSizeUniformLocation = GL.GetUniformLocation(downsampleProgram, "uOutputSize");
        blurSizeUniformLocation = GL.GetUniformLocation(blurProgram, "uSize");
        blurVerticalUniformLocation = GL.GetUniformLocation(blurProgram, "uVertical");
        upsampleOutputSizeUniformLocation = GL.GetUniformLocation(upsampleProgram, "uOutputSize");
        upsampleDiffusionUniformLocation = GL.GetUniformLocation(upsampleProgram, "uDiffusion");
        combineSizeUniformLocation = GL.GetUniformLocation(combineProgram, "uSize");
        combineIntensityUniformLocation = GL.GetUniformLocation(combineProgram, "uIntensity");
        
        GL.CreateSamplers(1, out textureSampler);
        GL.SamplerParameter(textureSampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameter(textureSampler, SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameter(textureSampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameter(textureSampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
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
        GL.UseProgram(prefilterProgram);
        
        GL.BindImageTexture(0, inputTexture, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
        GL.BindImageTexture(1, mipChain[0], 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba16f);
        GL.Uniform2(prefilterSizeUniformLocation, size);
        
        GL.DispatchCompute(
            MathUtil.DivideCeil(size.X, 8), 
            MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        
        // Bind sampler
        GL.BindSampler(0, textureSampler);
        
        // Downsample and blur until we reach MaxMipLevels
        for (var i = 1; i < mipChain.Count; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            // Downsample it to temporary image
            GL.UseProgram(downsampleProgram);
            GL.BindTextureUnit(0, mipChain[i - 1]);
            GL.BindImageTexture(1, tempImage1, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba16f);
            GL.Uniform2(downsampleOutputSizeUniformLocation, mipSize);
            
            GL.DispatchCompute(
                MathUtil.DivideCeil(mipSize.X, 8), 
                MathUtil.DivideCeil(mipSize.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            
            // Blur pass H
            GL.UseProgram(blurProgram);
            GL.BindImageTexture(0, tempImage1, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba16f);
            GL.BindImageTexture(1, tempImage2, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba16f);
            GL.Uniform2(blurSizeUniformLocation, mipSize);
            GL.Uniform1(blurVerticalUniformLocation, 0);
            
            GL.DispatchCompute(
                MathUtil.DivideCeil(mipSize.X, 8), 
                MathUtil.DivideCeil(mipSize.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            
            // Blur pass V
            GL.BindImageTexture(0, tempImage2, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba16f);
            GL.BindImageTexture(1, mipChain[i], 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba16f);
            GL.Uniform1(blurVerticalUniformLocation, 1);
            
            GL.DispatchCompute(
                MathUtil.DivideCeil(mipSize.X, 8), 
                MathUtil.DivideCeil(mipSize.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }
        
        // Upsample back up the chain
        GL.UseProgram(upsampleProgram);
        
        for (var i = mipChain.Count - 2; i >= 0; i--)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            GL.BindTextureUnit(0, mipChain[i + 1]);
            GL.BindImageTexture(1, mipChain[i], 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba16f);
            GL.Uniform2(upsampleOutputSizeUniformLocation, mipSize);
            GL.Uniform1(upsampleDiffusionUniformLocation, diffusion);
            
            GL.DispatchCompute(
                MathUtil.DivideCeil(mipSize.X, 8), 
                MathUtil.DivideCeil(mipSize.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }
        
        // Combine result with input
        GL.UseProgram(combineProgram);
        GL.BindImageTexture(0, inputTexture, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
        GL.BindImageTexture(1, mipChain[0], 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba16f);
        GL.BindImageTexture(2, outputTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);
        GL.Uniform2(combineSizeUniformLocation, size);
        GL.Uniform1(combineIntensityUniformLocation, intensity);
        
        GL.DispatchCompute(
            MathUtil.DivideCeil(size.X, 8), 
            MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        
        return true;
    }

    private void UpdateTextures(Vector2i size)
    {
        // Update the temporary images
        if (tempImage1 != 0)
            GL.DeleteTexture(tempImage1);
        GL.CreateTextures(TextureTarget.Texture2D, 1, out tempImage1);
        GL.TextureStorage2D(tempImage1, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        if (tempImage2 != 0)
            GL.DeleteTexture(tempImage2);
        GL.CreateTextures(TextureTarget.Texture2D, 1, out tempImage2);
        GL.TextureStorage2D(tempImage2, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        // Caluculate the mip levels
        var mipLevels = CalculateMipLevels(size);
        
        // Clean up old mip chain
        foreach (var mip in mipChain)
            GL.DeleteTexture(mip);
        mipChain.Clear();
        
        // Create new mip chain
        for (var i = 0; i < mipLevels; i++)
        {
            GL.CreateTextures(TextureTarget.Texture2D, 1, out int mip);
            GL.TextureStorage2D(mip, 1, SizedInternalFormat.Rgba16f, size.X >> i, size.Y >> i);
            mipChain.Add(mip);
        }
    }
    
    private static int CalculateMipLevels(Vector2i size)
    {
        var maxDim = Math.Max(size.X, size.Y);
        return (int) MathF.Floor(MathF.Log(maxDim, 2));
    }
    
    private static int CreateProgram(string shaderName)
    {
        var shaderSource = ResourceUtil.ReadAllText($"Resources.Shaders.PostProcessing.{shaderName}.glsl");
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);
        
        var program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        
        GL.DetachShader(program, shader);
        GL.DeleteShader(shader);
        
        return program;
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    public void Dispose()
    {
        GL.DeleteProgram(prefilterProgram);
        GL.DeleteProgram(downsampleProgram);
        GL.DeleteProgram(blurProgram);
        GL.DeleteProgram(upsampleProgram);
        GL.DeleteProgram(combineProgram);
        
        GL.DeleteTexture(tempImage1);
        GL.DeleteTexture(tempImage2);
        
        foreach (var mip in mipChain)
            GL.DeleteTexture(mip);
        
        GL.DeleteSampler(textureSampler);
    }
}