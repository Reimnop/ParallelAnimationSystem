using OpenTK;
using static SDL.SDL3;

namespace ParallelAnimationSystem.Android;

public class SDLBindingsContext : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => SDL_GL_GetProcAddress(procName);
}