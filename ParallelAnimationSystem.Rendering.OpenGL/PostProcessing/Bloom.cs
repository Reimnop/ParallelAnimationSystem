using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

public class Bloom : IDisposable
{
    private record struct Mip(int Handle, Vector2i Size);
    
    private readonly int prefilterProgram, downsampleProgram, upsampleProgram, combineProgram;
    private readonly int
        prefilterThresholdUniformLocation,
        prefilterCurveUniformLocation,
        upsampleSampleScaleUniformLocation,
        combineIntensityUniformLocation;
    
    private readonly int textureSampler;

    private readonly List<Mip> mipChain = [];
    private Vector2i currentSize = -Vector2i.One;

    public Bloom(ResourceLoader loader)
    {
        prefilterProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/BloomPrefilter");
        downsampleProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/BloomDownsample");
        upsampleProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/BloomUpsample");
        combineProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/BloomCombine");
        
        prefilterThresholdUniformLocation = GL.GetUniformLocation(prefilterProgram, "uThreshold");
        prefilterCurveUniformLocation = GL.GetUniformLocation(prefilterProgram, "uCurve");
        upsampleSampleScaleUniformLocation = GL.GetUniformLocation(upsampleProgram, "uSampleScale");
        combineIntensityUniformLocation = GL.GetUniformLocation(combineProgram, "uIntensity");
        
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

    public bool Process(Vector2i size, float intensity, float diffusion, int inputTexture, int outputTexture)
    {
        if (intensity == 0.0f)
            return false;

        intensity = MathF.Pow(2.0f, intensity / 10.0f) - 1.0f;
        
        // Determine iteration count
        var s = MathF.Max(size.X, size.Y);
        var logs = MathF.Log2(s) + MathF.Min(diffusion, 10f) - 10f; // Use 10 as base
        var logsInt = MathF.Floor(logs);
        var iterations = (int) Math.Clamp(logsInt, 1, 16); // Limit to 16 levels
        
        // Update mip chain if size has changed
        if (size != currentSize)
        {
            currentSize = size;
            UpdateMipChain(size, iterations);
        }
        
        if (mipChain.Count == 0)
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
        var threshold = 0.95f; // TODO: expose as parameter
        var softKnee = 0.5f;
        
        var knee = threshold * softKnee + 1e-5f;
        var curve0 = threshold - knee;
        var curve1 = knee * 2.0f;
        var curve2 = 0.25f / knee;
        
        GL.Uniform1f(prefilterThresholdUniformLocation, threshold);
        GL.Uniform3f(prefilterCurveUniformLocation, curve0, curve1, curve2);
        
        GL.DispatchCompute(
            (uint)MathUtil.DivideCeil(mip0.Size.X, 8), 
            (uint)MathUtil.DivideCeil(mip0.Size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        
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
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }
        
        // Upsample back up the chain
        GL.UseProgram(upsampleProgram);
        
        // Set sample scale uniform
        var sampleScale = 0.5f + (logs - logsInt);
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
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }
        
        // Combine result with input
        GL.UseProgram(combineProgram);
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f); // Bind output image
        
        GL.BindTextureUnit(0, inputTexture);
        GL.BindTextureUnit(1, mip0.Handle);
        
        GL.Uniform1f(combineIntensityUniformLocation, intensity);
        
        GL.DispatchCompute(
           (uint)MathUtil.DivideCeil(size.X, 8), 
           (uint)MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        
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
            var mipSize = new Vector2i(size.X >> i, size.Y >> i);
            
            var mipHandle = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(mipHandle, 1, SizedInternalFormat.Rgba16f, mipSize.X, mipSize.Y);
            mipChain.Add(new Mip(mipHandle, mipSize));
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