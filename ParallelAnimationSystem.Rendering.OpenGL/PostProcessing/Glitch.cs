using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

// based on limitless glitch 2
public class Glitch : IDisposable
{
    private const int TextureBinding = 0;
    private const int NoiseTextureBinding = 1;

    private const int NoiseTextureWidth = 64;
    private const int NoiseTextureHeight = 62;
    
    private readonly int program;
    private readonly int intensityUniformLocation;
    private readonly int colorIntensityUniformLocation;

    private readonly int noiseTexture;
    private readonly int noiseTextureSampler;
    private ulong currentNoiseSeed;
    private float currentStretchMultiplier;

    private readonly int sampler;
    
    private readonly byte[] noiseBuffer = new byte[NoiseTextureWidth * NoiseTextureHeight * 3];
 
    public Glitch(ResourceLoader loader)
    {
        program = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/Glitch");
        
        // Get uniform locations
        intensityUniformLocation = GL.GetUniformLocation(program, "uIntensity");
        colorIntensityUniformLocation = GL.GetUniformLocation(program, "uColorIntensity");
        
        var textureUniformLocation = GL.GetUniformLocation(program, "uTexture");
        var noiseTextureUniformLocation = GL.GetUniformLocation(program, "uNoiseTexture");
        
        // Set sampler uniform binding
        GL.UseProgram(program);
        GL.Uniform1i(textureUniformLocation, TextureBinding);
        GL.Uniform1i(noiseTextureUniformLocation, NoiseTextureBinding);
        
        // Create noise texture
        noiseTexture = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(noiseTexture, 1, SizedInternalFormat.Rgb8, NoiseTextureWidth, NoiseTextureHeight);
        
        // Create noise sampler
        noiseTextureSampler = GL.CreateSampler();
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        // Create main sampler
        sampler = GL.CreateSampler();
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }
    
    public void Dispose()
    {
        GL.DeleteProgram(program);
        GL.DeleteTexture(noiseTexture);
        GL.DeleteSampler(noiseTextureSampler);
        GL.DeleteSampler(sampler);
    }

    public bool Process(
        Vector2i size,
        float time,
        float speed, float intensity, float amount, float stretchMultiplier,
        int inputTexture, int outputTexture)
    {
        if (intensity == 0f || amount == 0f)
            return false;
        
        var updateFrequency = (0.1f + 0.4f * speed) * 60f; // assume 60 FPS as base
        var step = (int)MathF.Floor(time * updateFrequency); // snap time to intervals

        var noiseSeed = NumberUtil.ComputeHash(step);
        if (noiseSeed != currentNoiseSeed || stretchMultiplier != currentStretchMultiplier)
        {
            currentNoiseSeed = noiseSeed;
            currentStretchMultiplier = stretchMultiplier;
            
            var rng = NumberUtil.CreatePseudoRng(currentNoiseSeed);
            FillNoiseTexture(rng, currentStretchMultiplier);
        }
        
        GL.UseProgram(program);
        GL.Uniform1f(intensityUniformLocation, amount);
        GL.Uniform1f(colorIntensityUniformLocation, intensity);
        
        GL.BindSampler(TextureBinding, sampler);
        GL.BindTextureUnit(TextureBinding, inputTexture);
        
        GL.BindSampler(NoiseTextureBinding, noiseTextureSampler);
        GL.BindTextureUnit(NoiseTextureBinding, noiseTexture);
        
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        
        GL.DispatchCompute(
            (uint)MathUtil.DivideCeil(size.X, 8), 
            (uint)MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        return true;
    }

    private void FillNoiseTexture(PseudoRng rng, float stretchMultiplier)
    {
        GetRandomRgb(rng, out var r, out var g, out var b);
        for (var y = 0; y < NoiseTextureHeight; y++)
        {
            for (var x = 0; x < NoiseTextureWidth; x++)
            {
                if (GetRandomFloat(rng) > stretchMultiplier)
                    GetRandomRgb(rng, out r, out g, out b);
                
                var offset = (y * NoiseTextureWidth + x) * 3;
                noiseBuffer[offset] = r;
                noiseBuffer[offset + 1] = g;
                noiseBuffer[offset + 2] = b;
            }
        }
        
        // Upload data to GPU
        GL.PixelStorei(PixelStoreParameter.PackAlignment, 1);
        GL.TextureSubImage2D(noiseTexture, 0, 0, 0, NoiseTextureWidth, NoiseTextureHeight, PixelFormat.Rgb, PixelType.UnsignedByte, noiseBuffer);
    }

    private static float GetRandomFloat(PseudoRng rng)
        => NumberUtil.UlongToFloat01(rng());

    private static void GetRandomRgb(PseudoRng rng, out byte r, out byte g, out byte b)
    {
        unchecked
        {
            var value = rng();
            r = (byte)(value & 0xFF);
            g = (byte)((value >> 8) & 0xFF);
            b = (byte)((value >> 16) & 0xFF);
        }
    }
}