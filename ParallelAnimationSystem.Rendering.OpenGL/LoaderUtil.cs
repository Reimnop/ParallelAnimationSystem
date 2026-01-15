using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public static class LoaderUtil
{
    public static int LoadComputeProgram(ResourceLoader loader, string shaderName)
    {
        var shaderSource = loader.ReadResourceString($"Shaders/PostProcessing/{shaderName}.glsl");
        if (shaderSource is null)
            throw new InvalidOperationException($"Could not load shader source for '{shaderName}'");
        
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, shaderSource);
        GL.CompileShader(shader);
        
        var compileStatus = GL.GetShaderi(shader, ShaderParameterName.CompileStatus);
        if (compileStatus == 0)
        {
            GL.GetShaderInfoLog(shader, out var infoLog);
            throw new Exception($"Failed to compile shader: {infoLog}");
        }
        
        var program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        
        var linkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (linkStatus == 0)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new Exception($"Failed to link program: {infoLog}");
        }
        
        GL.DeleteShader(shader);
        
        return program;
    }
}