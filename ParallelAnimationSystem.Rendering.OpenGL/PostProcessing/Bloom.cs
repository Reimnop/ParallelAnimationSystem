using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

public class Bloom : IDisposable
{
    private readonly int prefilterProgram, downsampleProgram, upsampleProgram, combineProgram;
    private readonly int 
        prefilterSizeUniformLocation,
        prefilterThresholdUniformLocation,
        downsampleSizeUniformLocation,
        upsampleLowMipSamplerUniformLocation,
        upsampleHighMipSamplerUniformLocation,
        upsampleOutputSizeUniformLocation,
        upsampleDiffusionUniformLocation,
        combineSizeUniformLocation,
        combineIntensityUniformLocation;
    
    private readonly int textureSampler;
    
    private readonly List<int> downsampleMipChain = [];
    private readonly List<int> upsampleMipChain = [];
    private Vector2i currentSize = -Vector2i.One;

    public Bloom(ResourceLoader loader)
    {
        prefilterProgram = LoaderUtil.LoadComputeProgram(loader, "BloomPrefilter");
        downsampleProgram = LoaderUtil.LoadComputeProgram(loader, "BloomDownsample");
        upsampleProgram = LoaderUtil.LoadComputeProgram(loader, "BloomUpsample");
        combineProgram = LoaderUtil.LoadComputeProgram(loader, "BloomCombine");
        
        prefilterSizeUniformLocation = GL.GetUniformLocation(prefilterProgram, "uSize");
        prefilterThresholdUniformLocation = GL.GetUniformLocation(prefilterProgram, "uThreshold");
        downsampleSizeUniformLocation = GL.GetUniformLocation(downsampleProgram, "uSize");
        upsampleLowMipSamplerUniformLocation = GL.GetUniformLocation(upsampleProgram, "uLowMipSampler");
        upsampleHighMipSamplerUniformLocation = GL.GetUniformLocation(upsampleProgram, "uHighMipSampler");
        upsampleOutputSizeUniformLocation = GL.GetUniformLocation(upsampleProgram, "uOutputSize");
        upsampleDiffusionUniformLocation = GL.GetUniformLocation(upsampleProgram, "uDiffusion");
        combineSizeUniformLocation = GL.GetUniformLocation(combineProgram, "uSize");
        combineIntensityUniformLocation = GL.GetUniformLocation(combineProgram, "uIntensity");
        
        textureSampler = GL.CreateSampler();
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
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
        GL.UseProgram(prefilterProgram);
        
        GL.BindImageTexture(0, inputTexture, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
        GL.BindImageTexture(1, downsampleMipChain[0], 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        GL.Uniform2i(prefilterSizeUniformLocation, size.X, size.Y);
        GL.Uniform1f(prefilterThresholdUniformLocation, 0.95f);
        
        GL.DispatchCompute(
            (uint)MathUtil.DivideCeil(size.X, 8), 
            (uint)MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        
        // Downsample until we reach MaxMipLevels
        GL.UseProgram(downsampleProgram);
        
        for (var i = 1; i < downsampleMipChain.Count; i++)
        {
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            // Downsample pass (mip[i - 1] -> mip[i])
            GL.BindImageTexture(0, downsampleMipChain[i], 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
            GL.BindTextureUnit(0, downsampleMipChain[i - 1]);
            GL.Uniform2i(downsampleSizeUniformLocation, mipSize.X, mipSize.Y);
            
            GL.DispatchCompute(
                (uint)MathUtil.DivideCeil(mipSize.X, 8), 
                (uint)MathUtil.DivideCeil(mipSize.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }
        
        // Upsample back up the chain
        GL.UseProgram(upsampleProgram);
        
        GL.Uniform1i(upsampleLowMipSamplerUniformLocation, 0);
        GL.Uniform1i(upsampleHighMipSamplerUniformLocation, 1);
        
        for (var i = 0; i < upsampleMipChain.Count; i++)
        {
            var lowMip = i == 0 ? downsampleMipChain[^1] : upsampleMipChain[i - 1];
            var highMip = downsampleMipChain[^(i + 2)];
            var outputMip = upsampleMipChain[i];
            var outputSize = new Vector2i(size.X >> (upsampleMipChain.Count - i - 1), size.Y >> (upsampleMipChain.Count - i - 1));
            
            GL.BindImageTexture(0, outputMip, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
            GL.BindTextureUnit(0, lowMip);
            GL.BindTextureUnit(1, highMip);
            GL.Uniform2i(upsampleOutputSizeUniformLocation, outputSize.X, outputSize.Y);
            GL.Uniform1f(upsampleDiffusionUniformLocation, diffusion);
            
            GL.DispatchCompute(
                (uint)MathUtil.DivideCeil(outputSize.X, 8), 
                (uint)MathUtil.DivideCeil(outputSize.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }
        
        // Combine result with input
        GL.UseProgram(combineProgram);
        GL.BindImageTexture(0, inputTexture, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
        GL.BindImageTexture(1, upsampleMipChain[^1], 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
        GL.BindImageTexture(2, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        GL.Uniform2i(combineSizeUniformLocation, size.X, size.Y);
        GL.Uniform1f(combineIntensityUniformLocation, intensity);
        
        GL.DispatchCompute(
           (uint)MathUtil.DivideCeil(size.X, 8), 
           (uint)MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        
        return true;
    }

    private void UpdateTextures(Vector2i size)
    {
        // Caluculate the mip levels
        var mipLevels = CalculateMipLevels(size);
        
        // Clean up old mip chain
        foreach (var mip in downsampleMipChain)
            GL.DeleteTexture(mip);
        foreach (var mip in upsampleMipChain)
            GL.DeleteTexture(mip);
        
        downsampleMipChain.Clear();
        upsampleMipChain.Clear();
        
        // Create new mip chain
        for (var i = 0; i < mipLevels; i++)
        {
            var mip = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(mip, 1, SizedInternalFormat.Rgba16f, size.X >> i, size.Y >> i);
            downsampleMipChain.Add(mip);
        }
        
        // Create upsampling mip chain
        for (var i = mipLevels - 2; i >= 0; i--)
        {
            var mip = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(mip, 1, SizedInternalFormat.Rgba16f, size.X >> i, size.Y >> i);
            upsampleMipChain.Add(mip);
        }
    }
    
    private static int CalculateMipLevels(Vector2i size)
    {
        var minDim = Math.Min(size.X, size.Y);
        return (int) MathF.Floor(MathF.Log(minDim, 2));
    }

    public void Dispose()
    {
        GL.DeleteProgram(prefilterProgram);
        GL.DeleteProgram(upsampleProgram);
        GL.DeleteProgram(combineProgram);
        
        foreach (var mip in downsampleMipChain)
            GL.DeleteTexture(mip);
        
        foreach (var mip in upsampleMipChain)
            GL.DeleteTexture(mip);
        
        GL.DeleteSampler(textureSampler);
    }
}