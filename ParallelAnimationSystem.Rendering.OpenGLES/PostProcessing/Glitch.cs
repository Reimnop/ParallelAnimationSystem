using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

// based on limitless glitch 2
public class Glitch : IDisposable
{
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
    
    private readonly int framebuffer;
    private readonly int vaoHandle;
    
    private readonly byte[] noiseBuffer = new byte[NoiseTextureWidth * NoiseTextureHeight * 3];

    public Glitch(ResourceLoader loader, int vertexShader)
    {
        program = LoaderUtil.LoadPostProcessingProgram(loader, "Glitch", vertexShader);
        
        // Get uniform locations
        intensityUniformLocation = GL.GetUniformLocation(program, "uIntensity");
        colorIntensityUniformLocation = GL.GetUniformLocation(program, "uColorIntensity");
        
        var textureUniformLocation = GL.GetUniformLocation(program, "uTexture");
        var noiseTextureUniformLocation = GL.GetUniformLocation(program, "uNoiseTexture");
        
        // Set sampler uniform binding
        GL.UseProgram(program);
        GL.Uniform1i(textureUniformLocation, 0);
        GL.Uniform1i(noiseTextureUniformLocation, 1);
        
        // Create noise texture
        noiseTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, noiseTexture);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgb8, NoiseTextureWidth, NoiseTextureHeight);
        
        // Create noise sampler
        noiseTextureSampler = GL.GenSampler();
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(noiseTextureSampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        // Create main sampler
        sampler = GL.GenSampler();
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        // Initialize framebuffer and VAO
        framebuffer = GL.GenFramebuffer();
        vaoHandle = GL.GenVertexArray();
    }
    
    public void Dispose()
    {
        GL.DeleteProgram(program);
        GL.DeleteTexture(noiseTexture);
        GL.DeleteSampler(noiseTextureSampler);
        GL.DeleteSampler(sampler);
        GL.DeleteFramebuffer(framebuffer);
        GL.DeleteVertexArray(vaoHandle);
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
        
        // Attach output texture to framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, outputTexture, 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        GL.UseProgram(program);
        GL.Uniform1f(intensityUniformLocation, amount);
        GL.Uniform1f(colorIntensityUniformLocation, intensity);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        GL.BindSampler(0, sampler);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, noiseTexture);
        GL.BindSampler(1, noiseTextureSampler);
        
        GL.BindVertexArray(vaoHandle);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    private void FillNoiseTexture(PseudoRng rng, float stretchMultiplier)
    {
        GetRandomRgb(rng, out var colorR, out var colorG, out var colorB);
        for (var y = 0; y < NoiseTextureHeight; y++)
        {
            for (var x = 0; x < NoiseTextureWidth; x++)
            {
                if (GetRandomFloat(rng) > stretchMultiplier)
                    GetRandomRgb(rng, out colorR, out colorG, out colorB);
                
                var offset = (y * NoiseTextureWidth + x) * 3;
                noiseBuffer[offset] = colorR;
                noiseBuffer[offset + 1] = colorG;
                noiseBuffer[offset + 2] = colorB;
            }
        }
        
        // Upload data to GPU
        GL.PixelStorei(PixelStoreParameter.PackAlignment, 1);
        GL.BindTexture(TextureTarget.Texture2d, noiseTexture);
        GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, NoiseTextureWidth, NoiseTextureHeight, PixelFormat.Rgb, PixelType.UnsignedByte, noiseBuffer);
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
