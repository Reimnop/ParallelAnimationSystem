using OpenTK.Graphics.OpenGL;
using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

public class UberPost : IDisposable
{
    private readonly int program;
    
    private readonly int hueShiftAngleUniformLocation;
    
    private readonly int lensDistortionIntensityUniformLocation;
    private readonly int lensDistortionCenterUniformLocation;
    
    private readonly int chromaticAberrationIntensityUniformLocation;
    
    private readonly int vignetteCenterUniformLocation;
    private readonly int vignetteIntensityUniformLocation;
    private readonly int vignetteRoundnessUniformLocation;
    private readonly int vignetteSmoothnessUniformLocation;
    private readonly int vignetteColorUniformLocation;
    private readonly int vignetteModeUniformLocation;
    
    private readonly int gradientColor1UniformLocation;
    private readonly int gradientColor2UniformLocation;
    private readonly int gradientIntensityUniformLocation;
    private readonly int gradientRotationUniformLocation;
    private readonly int gradientModeUniformLocation;
    
    private readonly int sampler;

    public UberPost(ResourceLoader loader)
    {
        program = LoaderUtil.LoadComputeProgram(loader, "PostProcessing/UberPost");
        
        // Get uniform locations
        hueShiftAngleUniformLocation = GL.GetUniformLocation(program, "uHueShiftAngle");
        
        lensDistortionIntensityUniformLocation = GL.GetUniformLocation(program, "uLensDistortionIntensity");
        lensDistortionCenterUniformLocation = GL.GetUniformLocation(program, "uLensDistortionCenter");
        
        chromaticAberrationIntensityUniformLocation = GL.GetUniformLocation(program, "uChromaticAberrationIntensity");
        
        vignetteCenterUniformLocation = GL.GetUniformLocation(program, "uVignetteCenter");
        vignetteIntensityUniformLocation = GL.GetUniformLocation(program, "uVignetteIntensity");
        vignetteRoundnessUniformLocation = GL.GetUniformLocation(program, "uVignetteRoundness");
        vignetteSmoothnessUniformLocation = GL.GetUniformLocation(program, "uVignetteSmoothness");
        vignetteColorUniformLocation = GL.GetUniformLocation(program, "uVignetteColor");
        vignetteModeUniformLocation = GL.GetUniformLocation(program, "uVignetteMode");
        
        gradientColor1UniformLocation = GL.GetUniformLocation(program, "uGradientColor1");
        gradientColor2UniformLocation = GL.GetUniformLocation(program, "uGradientColor2");
        gradientIntensityUniformLocation = GL.GetUniformLocation(program, "uGradientIntensity");
        gradientRotationUniformLocation = GL.GetUniformLocation(program, "uGradientRotation");
        gradientModeUniformLocation = GL.GetUniformLocation(program, "uGradientMode");
        
        // Initialize sampler
        sampler = GL.CreateSampler();
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
    }
    
    public bool Process(
        Vector2i size,
        float hueShiftAngle,
        float lensDistortionIntensity, Vector2 lensDistortionCenter,
        float chromaticAberrationIntensity,
        Vector2 vignetteCenter, float vignetteIntensity, bool vignetteRounded, float vignetteRoundness, float vignetteSmoothness, ColorRgb vignetteColor, VignetteMode vignetteMode,
        ColorRgb gradientColor1, ColorRgb gradientColor2, float gradientIntensity, float gradientRotation, GradientOverlayMode gradientMode,
        int inputTexture, int outputTexture)
    {
        GL.UseProgram(program);
        
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        GL.BindTextureUnit(0, inputTexture);
        GL.BindSampler(0, sampler);
        
        GL.Uniform1f(hueShiftAngleUniformLocation, hueShiftAngle);
        
        GL.Uniform1f(lensDistortionIntensityUniformLocation, lensDistortionIntensity);
        GL.Uniform2f(lensDistortionCenterUniformLocation, 1, lensDistortionCenter);
        
        GL.Uniform1f(chromaticAberrationIntensityUniformLocation, chromaticAberrationIntensity);
        
        GL.Uniform2f(vignetteCenterUniformLocation, vignetteCenter.X, vignetteCenter.Y);
        GL.Uniform1f(vignetteIntensityUniformLocation, vignetteIntensity * 3);
        
        if (vignetteMode == VignetteMode.UseRounded)
        {
            GL.Uniform1f(vignetteRoundnessUniformLocation, vignetteRounded ? 1f : 0f);
            GL.Uniform1i(vignetteModeUniformLocation, 0);
        }
        else
        {
            var roundness = (1f - vignetteRoundness) * 6f + vignetteRoundness;
            GL.Uniform1f(vignetteRoundnessUniformLocation, roundness);
            GL.Uniform1i(vignetteModeUniformLocation, 1);
        }
        
        GL.Uniform1f(vignetteSmoothnessUniformLocation, vignetteSmoothness * 5f);
        GL.Uniform3f(vignetteColorUniformLocation, vignetteColor.R, vignetteColor.G, vignetteColor.B);
        
        GL.Uniform3f(gradientColor1UniformLocation, gradientColor1.R, gradientColor1.G, gradientColor1.B);
        GL.Uniform3f(gradientColor2UniformLocation, gradientColor2.R, gradientColor2.G, gradientColor2.B);
        GL.Uniform1f(gradientIntensityUniformLocation, gradientIntensity);

        unsafe
        {
            var rotationMatrix = stackalloc float[4];
            rotationMatrix[0] = MathF.Cos(gradientRotation);
            rotationMatrix[1] = -MathF.Sin(gradientRotation);
            rotationMatrix[2] = MathF.Sin(gradientRotation);
            rotationMatrix[3] = MathF.Cos(gradientRotation);
            GL.UniformMatrix2fv(gradientRotationUniformLocation, 1, false, rotationMatrix);
        }
        
        GL.Uniform1i(gradientModeUniformLocation, (int) gradientMode);
        
        GL.DispatchCompute(
            (uint)MathUtil.DivideCeil(size.X, 8), 
            (uint)MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        
        return true;
    }

    public void Dispose()
    {
        GL.DeleteProgram(program);
        GL.DeleteSampler(sampler);
    }
}