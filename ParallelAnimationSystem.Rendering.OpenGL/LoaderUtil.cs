using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public static class LoaderUtil
{
    public static int LoadShaderProgram(ResourceLoader loader, string vertexShaderName, string fragmentShaderName)
    {
        var vertexShaderSource = loader.ReadResourceString($"Shaders/{vertexShaderName}.glsl");
        if (vertexShaderSource is null)
            throw new InvalidOperationException($"Could not load shader source for '{vertexShaderName}'");
        
        var fragmentShaderSource = loader.ReadResourceString($"Shaders/{fragmentShaderName}.glsl");
        if (fragmentShaderSource is null)
            throw new InvalidOperationException($"Could not load shader source for '{fragmentShaderName}'");
        
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        var compileStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (compileStatus == 0)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new Exception($"Failed to compile vertex shader: {infoLog}");
        }
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        compileStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (compileStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new Exception($"Failed to compile fragment shader: {infoLog}");
        }
        
        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        var linkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (linkStatus == 0)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new Exception($"Failed to link program: {infoLog}");
        }
        
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        return program;
    }
    
    public static int LoadPPProgram(ResourceLoader loader, string shaderName)
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