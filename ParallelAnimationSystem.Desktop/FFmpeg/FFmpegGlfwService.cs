using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Desktop.FFmpeg;

public class FFmpegGlfwService(OpenGLSettings glSettings, FFmpegSettings settings) : GlfwService(glSettings)
{
    protected override void SetWindowHints(int width, int height, string title, bool useEgl)
    {
        base.SetWindowHints(width, height, title, useEgl);
        
        // Set additional hints for FFmpeg compatibility
        GLFW.WindowHint(WindowHintBool.Resizable, false);
        GLFW.WindowHint(WindowHintBool.Visible, settings.EnablePreview);
    }
}