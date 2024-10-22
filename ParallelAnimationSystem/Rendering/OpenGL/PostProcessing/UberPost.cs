using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;

public class UberPost(IResourceManager resourceManager) : IDisposable
{
    private int program;
    private int sizeUniformLocation;
    private int hueShiftAngleUniformLocation;
    private int lensDistortionIntensityUniformLocation;
    private int lensDistortionCenterUniformLocation;
    private int chromaticAberrationIntensityUniformLocation;
    private int vignetteCenterUniformLocation;
    private int vignetteIntensityUniformLocation;
    private int vignetteRoundedUniformLocation;
    private int vignetteRoundnessUniformLocation;
    private int vignetteSmoothnessUniformLocation;
    private int vignetteColorUniformLocation;
    private int sampler;

    public void Initialize()
    {
        var shaderSource = resourceManager.LoadResourceString("OpenGL/Shaders/PostProcessing/UberPost.glsl");
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);
        
        var compileStatus = GL.GetShaderi(shader, ShaderParameterName.CompileStatus);
        if (compileStatus == 0)
        {
            GL.GetShaderInfoLog(shader, out var infoLog);
            throw new Exception($"Failed to compile shader: {infoLog}");
        }
        
        program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        
        var linkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (linkStatus == 0)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new Exception($"Failed to link program: {infoLog}");
        }
        
        // Clean up
        GL.DeleteShader(shader);
        
        // Get uniform locations
        sizeUniformLocation = GL.GetUniformLocation(program, "uSize");
        hueShiftAngleUniformLocation = GL.GetUniformLocation(program, "uHueShiftAngle");
        lensDistortionIntensityUniformLocation = GL.GetUniformLocation(program, "uLensDistortionIntensity");
        lensDistortionCenterUniformLocation = GL.GetUniformLocation(program, "uLensDistortionCenter");
        chromaticAberrationIntensityUniformLocation = GL.GetUniformLocation(program, "uChromaticAberrationIntensity");
        vignetteCenterUniformLocation = GL.GetUniformLocation(program, "uVignetteCenter");
        vignetteIntensityUniformLocation = GL.GetUniformLocation(program, "uVignetteIntensity");
        vignetteRoundedUniformLocation = GL.GetUniformLocation(program, "uVignetteRounded");
        vignetteRoundnessUniformLocation = GL.GetUniformLocation(program, "uVignetteRoundness");
        vignetteSmoothnessUniformLocation = GL.GetUniformLocation(program, "uVignetteSmoothness");
        vignetteColorUniformLocation = GL.GetUniformLocation(program, "uVignetteColor");
        
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
        Vector2 vignetteCenter, float vignetteIntensity, bool vignetteRounded, float vignetteRoundness, float vignetteSmoothness, Vector3 vignetteColor,
        int inputTexture, int outputTexture)
    {
        if (hueShiftAngle == 0.0f && lensDistortionIntensity == 0.0f && chromaticAberrationIntensity == 0.0f && vignetteIntensity == 0.0f)
            return false;
        
        GL.UseProgram(program);
        
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        GL.BindTextureUnit(0, inputTexture);
        GL.Uniform2i(sizeUniformLocation, 1, size);
        GL.Uniform1f(hueShiftAngleUniformLocation, hueShiftAngle);
        GL.Uniform1f(lensDistortionIntensityUniformLocation, lensDistortionIntensity);
        GL.Uniform2f(lensDistortionCenterUniformLocation, 1, lensDistortionCenter);
        GL.Uniform1f(chromaticAberrationIntensityUniformLocation, chromaticAberrationIntensity);
        GL.Uniform2f(vignetteCenterUniformLocation, 1, vignetteCenter);
        GL.Uniform1f(vignetteIntensityUniformLocation, vignetteIntensity * 3.0f);
        GL.Uniform1f(vignetteRoundedUniformLocation, vignetteRounded ? 1.0f : 0.0f);
        GL.Uniform1f(vignetteRoundnessUniformLocation, (1.0f - vignetteRoundness) * 6.0f + vignetteRoundness);
        GL.Uniform1f(vignetteSmoothnessUniformLocation, vignetteSmoothness * 5.0f);
        GL.Uniform3f(vignetteColorUniformLocation, 1, vignetteColor);
        
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