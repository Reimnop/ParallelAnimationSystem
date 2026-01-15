using Android.Views;

namespace ParallelAnimationSystem.Android;

public class AndroidSurfaceContext
{
    public required GraphicsSurfaceView SurfaceView { get; init; }
    public required ISurfaceHolder SurfaceHolder { get; init; }
}