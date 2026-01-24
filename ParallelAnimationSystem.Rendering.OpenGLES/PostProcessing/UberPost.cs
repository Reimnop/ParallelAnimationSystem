using OpenTK.Graphics.OpenGL;
using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;

public class UberPost : IDisposable
{
    private readonly int program;
    
    private readonly int timeUniformLocation;
    
    private readonly int hueShiftAngleUniformLocation;
    
    private readonly int lensDistortionIntensityUniformLocation;
    private readonly int lensDistortionCenterUniformLocation;
    
    private readonly int chromaticAberrationIntensityUniformLocation;
    
    private readonly int vignetteCenterUniformLocation;
    private readonly int vignetteIntensityUniformLocation;
    private readonly int vignetteRoundedUniformLocation;
    private readonly int vignetteRoundnessUniformLocation;
    private readonly int vignetteSmoothnessUniformLocation;
    private readonly int vignetteColorUniformLocation;
    
    private readonly int gradientColor1UniformLocation;
    private readonly int gradientColor2UniformLocation;
    private readonly int gradientIntensityUniformLocation;
    private readonly int gradientRotationUniformLocation;
    private readonly int gradientModeUniformLocation;
    
    private readonly int glitchIntensityUniformLocation;
    private readonly int glitchSpeedUniformLocation;
    private readonly int glitchSizeUniformLocation;
    
    private readonly int framebuffer;

    private readonly int vaoHandle;

    public UberPost(ResourceLoader loader, int vertexShader)
    {
        program = LoaderUtil.LoadPostProcessingProgram(loader, "UberPost", vertexShader);
        
        timeUniformLocation = GL.GetUniformLocation(program, "uTime");
        
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
        
        gradientColor1UniformLocation = GL.GetUniformLocation(program, "uGradientColor1");
        gradientColor2UniformLocation = GL.GetUniformLocation(program, "uGradientColor2");
        gradientIntensityUniformLocation = GL.GetUniformLocation(program, "uGradientIntensity");
        gradientRotationUniformLocation = GL.GetUniformLocation(program, "uGradientRotation");
        gradientModeUniformLocation = GL.GetUniformLocation(program, "uGradientMode");
        
        glitchIntensityUniformLocation = GL.GetUniformLocation(program, "uGlitchIntensity");
        glitchSpeedUniformLocation = GL.GetUniformLocation(program, "uGlitchSpeed");
        glitchSizeUniformLocation = GL.GetUniformLocation(program, "uGlitchSize");
        
        // Initialize framebuffer
        framebuffer = GL.GenFramebuffer();
        
        // Initialize a simple VAO
        vaoHandle = GL.GenVertexArray();
    }
    
    public bool Process(
        Vector2i size,
        float time,
        float hueShiftAngle,
        float lensDistortionIntensity,
        Vector2 lensDistortionCenter, 
        float chromaticAberrationIntensity,
        Vector2 vignetteCenter, float vignetteIntensity, bool vignetteRounded, float vignetteRoundness, float vignetteSmoothness, Vector3 vignetteColor,
        Vector3 gradientColor1, Vector3 gradientColor2, float gradientIntensity, float gradientRotation, GradientOverlayMode gradientMode,
        float glitchIntensity, float glitchSpeed, Vector2 glitchSize,
        int inputTexture, int outputTexture)
    {
        if (hueShiftAngle == 0.0f && 
            lensDistortionIntensity == 0.0f && 
            chromaticAberrationIntensity == 0.0f && 
            vignetteIntensity == 0.0f &&
            gradientIntensity == 0.0f &&
            glitchIntensity == 0.0f)
            return false;
        
        // Attach output texture to framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, outputTexture, 0);
        GL.Viewport(0, 0, size.X, size.Y);
        
        // Do our processing
        GL.UseProgram(program);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, inputTexture);
        
        GL.Uniform1f(timeUniformLocation, time);
        
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
        
        GL.Uniform3f(gradientColor1UniformLocation, 1, gradientColor1);
        GL.Uniform3f(gradientColor2UniformLocation, 1, gradientColor2);
        GL.Uniform1f(gradientIntensityUniformLocation, gradientIntensity);
        
        unsafe
        {
            var rotationMatrix = stackalloc float[4];
            rotationMatrix[0] = MathF.Cos(gradientRotation);
            rotationMatrix[1] = MathF.Sin(gradientRotation);
            rotationMatrix[2] = -MathF.Sin(gradientRotation);
            rotationMatrix[3] = MathF.Cos(gradientRotation);
            GL.UniformMatrix2fv(gradientRotationUniformLocation, 1, false, rotationMatrix);
        }
        
        GL.Uniform1i(gradientModeUniformLocation, (int) gradientMode);
        
        GL.Uniform1f(glitchIntensityUniformLocation, glitchIntensity);
        GL.Uniform1f(glitchSpeedUniformLocation, glitchSpeed);
        GL.Uniform2f(glitchSizeUniformLocation, 1, glitchSize);
        
        GL.BindVertexArray(vaoHandle);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        return true;
    }

    public void Dispose()
    {
        GL.DeleteProgram(program);
        GL.DeleteFramebuffer(framebuffer);
        GL.DeleteVertexArray(vaoHandle);
    }
}