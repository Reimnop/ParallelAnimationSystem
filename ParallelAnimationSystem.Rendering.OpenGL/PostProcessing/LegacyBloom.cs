using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

public class LegacyBloom : IDisposable
{
    private record struct Mip(int Handle, Vector2i Size);
    
    private readonly int prefilterProgram, downsampleProgram, upsampleProgram, combineProgram;
    private readonly int
        prefilterThresholdUniformLocation,
        prefilterKneeUniformLocation,
        upsampleSampleScaleUniformLocation,
        combineTintUniformLocation;
    
    private readonly int textureSampler;

    private readonly List<Mip> mipChain = [];
    private Vector2i currentSize = -Vector2i.One;
    private int currentIterations = 0;

    public LegacyBloom(ResourceLoader loader)
    {
        prefilterProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Bloom/Prefilter");
        downsampleProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Bloom/Legacy/Downsample");
        upsampleProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Bloom/Legacy/Upsample");
        combineProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Bloom/Combine");
        
        prefilterThresholdUniformLocation = GL.GetUniformLocation(prefilterProgram, "uThreshold");
        prefilterKneeUniformLocation = GL.GetUniformLocation(prefilterProgram, "uKnee");
        upsampleSampleScaleUniformLocation = GL.GetUniformLocation(upsampleProgram, "uSampleScale");
        combineTintUniformLocation = GL.GetUniformLocation(combineProgram, "uTint");
        
        var combineSourceSamplerUniformLocation = GL.GetUniformLocation(combineProgram, "uSourceSampler");
        var combineBloomSamplerUniformLocation = GL.GetUniformLocation(combineProgram, "uBloomSampler");
        
        // Set sampler uniform binding
        GL.UseProgram(combineProgram);
        GL.Uniform1i(combineSourceSamplerUniformLocation, 0);
        GL.Uniform1i(combineBloomSamplerUniformLocation, 1);
        
        textureSampler = GL.CreateSampler();
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(textureSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    public bool Process(Vector2i size, float intensity, float diffusion, ColorRgb color, int inputTexture, int outputTexture)
    {
        if (intensity == 0.0f)
            return false;

        intensity = MathF.Pow(2.0f, intensity / 10.0f) - 1.0f;
        
        // Determine iteration count
        var s = MathF.Max(size.X, size.Y);
        var logS = MathF.Log2(s) + MathF.Min(diffusion, 10f) - 10f; // Use 10 as base
        var logSFloored = MathF.Floor(logS);
        var iterations = (int) Math.Clamp(logSFloored, 1, 16); // Limit to 16 levels
        var sampleScale = 0.5f + (logS - logSFloored);
        
        // Update mip chain if size has changed
        if (size != currentSize || iterations != currentIterations)
        {
            currentSize = size;
            currentIterations = iterations;
            UpdateMipChain(size, iterations);
        }
        
        if (mipChain.Count < 2)
            return false;
        
        // Get mip 0
        var mip0 = mipChain[0];
        
        // Bind samplers
        GL.BindSampler(0, textureSampler);
        GL.BindSampler(1, textureSampler);
        
        // Prefilter to mip 0
        GL.UseProgram(prefilterProgram);
        
        GL.BindImageTexture(0, mip0.Handle, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f); // Bind output image
        GL.BindTextureUnit(0, inputTexture); // Bind input texture
        
        // Set knee and threshold uniforms
        var threshold = 0.9f; // TODO: expose as parameter
        var knee = 0.5f;
        
        GL.Uniform1f(prefilterThresholdUniformLocation, threshold);
        GL.Uniform1f(prefilterKneeUniformLocation, threshold * knee);
        
        GL.DispatchCompute(
            (uint)MathUtil.DivideCeil(mip0.Size.X, 8), 
            (uint)MathUtil.DivideCeil(mip0.Size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        
        // Downsample
        GL.UseProgram(downsampleProgram);
        
        for (var i = 1; i < mipChain.Count; i++)
        {
            var sourceMip = mipChain[i - 1];
            var targetMip = mipChain[i];
            
            // Downsample pass (mip[i - 1] -> mip[i])
            GL.BindImageTexture(0, targetMip.Handle, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f); // Bind output image
            GL.BindTextureUnit(0, sourceMip.Handle); // Bind input texture
            
            GL.DispatchCompute(
                (uint)MathUtil.DivideCeil(targetMip.Size.X, 8), 
                (uint)MathUtil.DivideCeil(targetMip.Size.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        }
        
        // Upsample back up the chain
        GL.UseProgram(upsampleProgram);
        
        // Set sample scale uniform
        GL.Uniform1f(upsampleSampleScaleUniformLocation, sampleScale);
        
        for (var i = mipChain.Count - 2; i >= 0; i--)
        {
            var sourceMip = mipChain[i + 1];
            var targetMip = mipChain[i];
            
            // Upsample pass (mip[i + 1] -> mip[i])
            GL.BindImageTexture(0, targetMip.Handle, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.Rgba16f); // Bind output image
            GL.BindTextureUnit(0, sourceMip.Handle); // Bind input texture
            
            GL.DispatchCompute(
                (uint)MathUtil.DivideCeil(targetMip.Size.X, 8), 
                (uint)MathUtil.DivideCeil(targetMip.Size.Y, 8), 
                1);
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        }
        
        // Combine result with input
        GL.UseProgram(combineProgram);
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f); // Bind output image
        
        GL.BindTextureUnit(0, inputTexture);
        GL.BindTextureUnit(1, mip0.Handle);

        var colorLinear = new ColorRgb(color.R * color.R, color.G * color.G, color.B * color.B);
        var colorLuminance = 0.2126f * colorLinear.R + 0.7152f * colorLinear.G + 0.0722f * colorLinear.B;
        colorLinear = colorLuminance > 0f ? colorLinear * (1f / colorLuminance) : new ColorRgb(1f, 1f, 1f);
        var tint = colorLinear * intensity;
        GL.Uniform3f(combineTintUniformLocation, tint.R, tint.G, tint.B);
        
        GL.DispatchCompute(
           (uint)MathUtil.DivideCeil(size.X, 8), 
           (uint)MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        
        return true;
    }

    private void UpdateMipChain(Vector2i size, int levels)
    {
        // Clean up old mip chain
        foreach (var mip in mipChain)
            GL.DeleteTexture(mip.Handle);
        
        mipChain.Clear();
        
        // Create new mip chain
        for (var i = 0; i < levels; i++)
        {
            var mipSize = new Vector2i(
                Math.Max(size.X >> i, 1),
                Math.Max(size.Y >> i, 1));
            
            var mipHandle = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(mipHandle, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            mipChain.Add(new Mip(mipHandle, mipSize));
            
            if (mipSize is { X: 1, Y: 1 })
                break; // Stop if we've reached 1x1
        }
    }

    public void Dispose()
    {
        GL.DeleteProgram(prefilterProgram);
        GL.DeleteProgram(downsampleProgram);
        GL.DeleteProgram(upsampleProgram);
        GL.DeleteProgram(combineProgram);
        
        GL.DeleteSampler(textureSampler);
        
        foreach (var mip in mipChain)
            GL.DeleteTexture(mip.Handle);
    }
}