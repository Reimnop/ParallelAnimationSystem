using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

public class Grain : IDisposable
{
    private const int NoiseTextureWidth = 128;
    private const int NoiseTextureHeight = 128;
    
    private readonly int noiseProgram, filterProgram, noiseAddProgram, compositeProgram;

    private readonly int
        noisePhaseUniformLocation,
        filterParamsUniformLocation,
        noiseAddChannelUniformLocation,
        compositeColoredUniformLocation,
        compositeLuminanceContributionUniformLocation,
        compositeIntensityUniformLocation,
        compositeScaleUniformLocation,
        compositeOffsetUniformLocation;

    private readonly int noiseTextureA, noiseTextureB, noiseTextureCombined;
    private readonly int noiseSampler, linearSampler, noiseSamplerLinearRepeat;

    public Grain(ResourceLoader loader)
    {
        // Load programs
        noiseProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Grain/Noise");
        filterProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Grain/Filter");
        noiseAddProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Grain/NoiseAdd");
        compositeProgram = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Grain/Composite");
        
        // Get uniform locations
        noisePhaseUniformLocation = GL.GetUniformLocation(noiseProgram, "uPhase");
        filterParamsUniformLocation = GL.GetUniformLocation(filterProgram, "uParams");
        noiseAddChannelUniformLocation = GL.GetUniformLocation(noiseAddProgram, "uChannel");
        compositeColoredUniformLocation = GL.GetUniformLocation(compositeProgram, "uColored");
        compositeLuminanceContributionUniformLocation = GL.GetUniformLocation(compositeProgram, "uLuminanceContribution");
        compositeIntensityUniformLocation = GL.GetUniformLocation(compositeProgram, "uIntensity");
        compositeScaleUniformLocation = GL.GetUniformLocation(compositeProgram, "uScale");
        compositeOffsetUniformLocation = GL.GetUniformLocation(compositeProgram, "uOffset");
        
        // Set binding locations
        var filterSourceSamplerUniformLocation = GL.GetUniformLocation(filterProgram, "uSourceSampler");
        GL.UseProgram(filterProgram);
        GL.Uniform1i(filterSourceSamplerUniformLocation, 0);
        
        var compositeSourceSamplerUniformLocation = GL.GetUniformLocation(compositeProgram, "uSourceSampler");
        var compositeNoiseSamplerUniformLocation = GL.GetUniformLocation(compositeProgram, "uNoiseSampler");
        GL.UseProgram(compositeProgram);
        GL.Uniform1i(compositeSourceSamplerUniformLocation, 0);
        GL.Uniform1i(compositeNoiseSamplerUniformLocation, 1);
        
        var noiseAddSourceSamplerUniformLocation = GL.GetUniformLocation(noiseAddProgram, "uSourceSampler");
        GL.UseProgram(noiseAddProgram);
        GL.Uniform1i(noiseAddSourceSamplerUniformLocation, 0);
        
        // Set up noise textures
        noiseTextureA = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(noiseTextureA, 1, SizedInternalFormat.R16f, NoiseTextureWidth, NoiseTextureHeight);
        
        noiseTextureB = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(noiseTextureB, 1, SizedInternalFormat.R16f, NoiseTextureWidth, NoiseTextureHeight);
        
        noiseTextureCombined = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(noiseTextureCombined, 1, SizedInternalFormat.Rgba16f, NoiseTextureWidth, NoiseTextureHeight);
        
        // Set up samplers
        noiseSampler = GL.CreateSampler();
        GL.SamplerParameteri(noiseSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.SamplerParameteri(noiseSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.SamplerParameteri(noiseSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.SamplerParameteri(noiseSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.Repeat);
        
        linearSampler = GL.CreateSampler();
        GL.SamplerParameteri(linearSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(linearSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(linearSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(linearSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        noiseSamplerLinearRepeat = GL.CreateSampler();
        GL.SamplerParameteri(noiseSamplerLinearRepeat, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(noiseSamplerLinearRepeat, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(noiseSamplerLinearRepeat, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.SamplerParameteri(noiseSamplerLinearRepeat, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.Repeat);
    }

    public void Dispose()
    {
        GL.DeleteProgram(noiseProgram);
        GL.DeleteProgram(filterProgram);
        GL.DeleteProgram(noiseAddProgram);
        GL.DeleteProgram(compositeProgram); 
        
        GL.DeleteTexture(noiseTextureA);
        GL.DeleteTexture(noiseTextureB);
        GL.DeleteTexture(noiseTextureCombined);
        
        GL.DeleteSampler(noiseSampler);
        GL.DeleteSampler(linearSampler);
        GL.DeleteSampler(noiseSamplerLinearRepeat);
    }

    public bool Process(
        Vector2i size, float time,
        bool colored, float intensity,
        float grainSize, float luminanceContribution,
        int inputTexture, int outputTexture)
    {
        if (intensity == 0f)
            return false;
        
        // Get noise texture for this frame
        GL.BindSampler(0, noiseSampler);
        var noiseTexture = ComputeNoise(time, colored);
        
        // Bind output image
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        
        // Bind samplers
        GL.BindSampler(0, linearSampler);
        GL.BindSampler(1, noiseSamplerLinearRepeat);
        
        // Bind input textures
        GL.BindTextureUnit(0, inputTexture);
        GL.BindTextureUnit(1, noiseTexture);
        
        // Set uniforms
        GL.UseProgram(compositeProgram);
        GL.Uniform1i(compositeColoredUniformLocation, colored ? 1 : 0);
        GL.Uniform1f(compositeLuminanceContributionUniformLocation, luminanceContribution);
        GL.Uniform1f(compositeIntensityUniformLocation, intensity * 20f);
        GL.Uniform2f(compositeScaleUniformLocation, 
            size.X / (float)NoiseTextureWidth / grainSize, 
            size.Y / (float)NoiseTextureHeight / grainSize);
        
        // Randomize noise offset each frame to reduce pattern repetition
        var rndX = NumberUtil.UlongToFloat01(NumberUtil.Mix(NumberUtil.ComputeHash(time), 0));
        var rndY = NumberUtil.UlongToFloat01(NumberUtil.Mix(NumberUtil.ComputeHash(time), 1));
        GL.Uniform2f(compositeOffsetUniformLocation, rndX, rndY);
        
        // Dispatch compute shader
        GL.DispatchCompute(
            (uint)MathUtil.DivideCeil(size.X, 8),
            (uint)MathUtil.DivideCeil(size.Y, 8),
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        
        return true;
    }

    private int ComputeNoise(float time, bool colored)
    {
        var phase = time - MathF.Floor(time);
        if (!colored)
            return ComputeNoiseSingleChannel(phase);
        
        // Generate noise for each channel with different phases
        var redChannel = ComputeNoiseSingleChannel(phase * 0.07f);
        ComputeCombinedNoiseTexture(redChannel, noiseTextureCombined, 0);
        
        var greenChannel = ComputeNoiseSingleChannel(phase * 0.11f);
        ComputeCombinedNoiseTexture(greenChannel, noiseTextureCombined, 1);
        
        var blueChannel = ComputeNoiseSingleChannel(phase * 0.13f);
        ComputeCombinedNoiseTexture(blueChannel, noiseTextureCombined, 2);
        
        return noiseTextureCombined;
    }

    private void ComputeCombinedNoiseTexture(int texture, int target, int channel)
    {
        GL.BindImageTexture(0, target, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.Rgba16f); // Bind output image
        
        GL.UseProgram(noiseAddProgram);
        GL.BindTextureUnit(0, texture); // Bind input texture

        GL.Uniform1i(noiseAddChannelUniformLocation, channel);
        GL.DispatchCompute(NoiseTextureWidth / 8, NoiseTextureHeight / 8, 1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
    }

    private int ComputeNoiseSingleChannel(float phase)
    {
        // Generate noise in noiseTextureA
        GL.BindImageTexture(0, noiseTextureA, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.R16f); // Bind output image
        
        GL.UseProgram(noiseProgram);
        GL.Uniform1f(noisePhaseUniformLocation, phase);
        
        GL.DispatchCompute(NoiseTextureWidth / 8, NoiseTextureHeight / 8, 1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        
        // Filter noise into noiseTextureB
        GL.BindImageTexture(0, noiseTextureB, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.R16f); // Bind output image
        
        GL.UseProgram(filterProgram);
        
        GL.BindTextureUnit(0, noiseTextureA); // Bind input texture
        GL.Uniform2f(filterParamsUniformLocation, 2f, -12f);
        
        GL.DispatchCompute(NoiseTextureWidth / 8, NoiseTextureHeight / 8, 1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        
        // Filter noise again into noiseTextureA
        GL.BindImageTexture(0, noiseTextureA, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.R16f); // Bind output image
        
        GL.BindTextureUnit(0, noiseTextureB); // Bind input texture
        GL.Uniform2f(filterParamsUniformLocation, 2f, 4f);
        
        GL.DispatchCompute(NoiseTextureWidth / 8, NoiseTextureHeight / 8, 1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        
        return noiseTextureA;
    }
}