using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public static class LoaderUtil
{
    public static int LoadShaderProgram(ResourceLoader loader, string vertexShaderName, string fragmentShaderName)
    {
        var vertexShaderSource = loader.ReadResourceString($"Shaders/{vertexShaderName}.glsl");
        if (vertexShaderSource is null)
            throw new InvalidOperationException($"Failed to load vertex shader source '{vertexShaderName}'");
        
        var fragmentShaderSource = loader.ReadResourceString($"Shaders/{fragmentShaderName}.glsl");
        if (fragmentShaderSource is null)
            throw new InvalidOperationException($"Failed to load fragment shader source '{fragmentShaderName}'");
        
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        var vertexShaderCompileStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (vertexShaderCompileStatus == 0)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new Exception($"Failed to compile vertex shader: {infoLog}");
        }
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
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
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        return program;
    }
    
    public static int LoadPostProcessingProgram(ResourceLoader loader, string shaderName, int vertexShader)
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