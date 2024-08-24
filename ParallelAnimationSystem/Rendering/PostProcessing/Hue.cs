using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.PostProcessing;

public class Hue : IDisposable
{
    private int program;
    private int sizeUniformLocation;
    private int hueShiftAngleUniformLocation;

    public void Initialize()
    {
        // We will use compute shaders to do post-processing
        var shaderSource = ResourceUtil.ReadAllText("Resources.Shaders.PostProcessing.Hue.glsl");
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);
        
        program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        
        // Get uniform locations
        sizeUniformLocation = GL.GetUniformLocation(program, "uSize");
        hueShiftAngleUniformLocation = GL.GetUniformLocation(program, "uHueShiftAngle");
        
        // Clean up
        GL.DetachShader(program, shader);
        GL.DeleteShader(shader);
    }
    
    public bool Process(Vector2i size, float shiftAngle, int inputTexture, int outputTexture)
    {
        if (shiftAngle == 0.0f)
            return false;
        
        GL.UseProgram(program);
        
        GL.BindImageTexture(0, inputTexture, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba8);
        GL.BindImageTexture(1, outputTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8);
        GL.Uniform2(sizeUniformLocation, size);
        GL.Uniform1(hueShiftAngleUniformLocation, shiftAngle);
        
        GL.DispatchCompute(
            MathUtil.DivideCeil(size.X, 8), 
            MathUtil.DivideCeil(size.Y, 8), 
            1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        
        return true;
    }

    public void Dispose()
    {
        GL.DeleteProgram(program);
    }
}