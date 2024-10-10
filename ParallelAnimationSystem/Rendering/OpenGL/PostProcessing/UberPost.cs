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
    private int sampler;

    public void Initialize()
    {
        var shaderSource = resourceManager.LoadResourceString("OpenGL/Shaders/PostProcessing/UberPost.glsl");
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);
        
        program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        
        // Get uniform locations
        sizeUniformLocation = GL.GetUniformLocation(program, "uSize");
        hueShiftAngleUniformLocation = GL.GetUniformLocation(program, "uHueShiftAngle");
        lensDistortionIntensityUniformLocation = GL.GetUniformLocation(program, "uLensDistortionIntensity");
        lensDistortionCenterUniformLocation = GL.GetUniformLocation(program, "uLensDistortionCenter");
        
        // Clean up
        GL.DeleteShader(shader);
        
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
        float lensDistortionIntensity,
        Vector2 lensDistortionCenter,
        int inputTexture, int outputTexture)
    {
        if (hueShiftAngle == 0.0f && lensDistortionIntensity == 0.0f)
            return false;
        
        GL.UseProgram(program);
        
        GL.BindImageTexture(0, outputTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
        GL.BindTextureUnit(0, inputTexture);
        GL.Uniform2i(sizeUniformLocation, 1, size);
        GL.Uniform1f(hueShiftAngleUniformLocation, hueShiftAngle);
        GL.Uniform1f(lensDistortionIntensityUniformLocation, lensDistortionIntensity);
        GL.Uniform2f(lensDistortionCenterUniformLocation, 1, lensDistortionCenter);
        
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
    }
}