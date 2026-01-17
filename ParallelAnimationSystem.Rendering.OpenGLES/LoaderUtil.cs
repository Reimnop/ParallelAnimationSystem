using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public static class LoaderUtil
{
    public static int LoadPPProgram(ResourceLoader loader, string shaderName, int vertexShader)
    {
        var shaderSource = loader.ReadResourceString($"Shaders/PostProcessing/{shaderName}.glsl");
        if (shaderSource is null)
            throw new Exception($"Failed to load fragment shader source '{shaderName}'");
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, shaderSource);
        GL.CompileShader(fragmentShader);
        
        var fragmentShaderCompileStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (fragmentShaderCompileStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new Exception($"Failed to compile fragment shader: {infoLog}");
        }
        
        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        var programLinkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (programLinkStatus == 0)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new Exception($"Failed to link program: {infoLog}");
        }
        
        // Clean up
        GL.DetachShader(program, vertexShader);
        GL.DeleteShader(fragmentShader);
        
        return program;
    }
}