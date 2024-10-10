using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

public class UberPost(IResourceManager resourceManager) : IDisposable
{
    private int program;
    private int sizeUniformLocation;
    private int hueShiftAngleUniformLocation;
    private int lensDistortionIntensityUniformLocation;
    private int lensDistortionCenterUniformLocation;
    
    private int framebuffer;

    public void Initialize(int vertexShader)
    {
        var shaderSource = resourceManager.LoadResourceString("OpenGLES/Shaders/PostProcessing/UberPost.glsl");
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, shaderSource);
        GL.CompileShader(fragmentShader);
        
        var fragmentShaderCompileStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (fragmentShaderCompileStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new Exception($"Failed to compile fragment shader: {infoLog}");
        }
        
        program = GL.CreateProgram();
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
        
        sizeUniformLocation = GL.GetUniformLocation(program, "uSize");
        hueShiftAngleUniformLocation = GL.GetUniformLocation(program, "uHueShiftAngle");
        lensDistortionIntensityUniformLocation = GL.GetUniformLocation(program, "uLensDistortionIntensity");
        lensDistortionCenterUniformLocation = GL.GetUniformLocation(program, "uLensDistortionCenter");
        
        // Initialize framebuffer
        framebuffer = GL.GenFramebuffer();
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
        
        // Attach output texture to framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, outputTexture, 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        // Do our processing
        GL.UseProgram(program);
        GL.Uniform2f(sizeUniformLocation, size.X, size.Y);
        GL.Uniform1f(hueShiftAngleUniformLocation, hueShiftAngle);
        GL.Uniform1f(lensDistortionIntensityUniformLocation, lensDistortionIntensity);
        GL.Uniform2f(lensDistortionCenterUniformLocation, 1, lensDistortionCenter);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    public void Dispose()
    {
        GL.DeleteProgram(program);
        GL.DeleteFramebuffer(framebuffer);
    }
}