using Android.Content;
using Android.Runtime;
using Android.Views;
using Org.Libsdl.App;

namespace ParallelAnimationSystem.Android;

public class AndroidSurface : SDLSurface
{
    public bool SurfaceReady { get; private set; }

    protected AndroidSurface(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public AndroidSurface(Context? p0) : base(p0)
    {
    }

    public override void SurfaceDestroyed(ISurfaceHolder? p0)
    {
        base.SurfaceDestroyed(p0);
        SurfaceReady = false;
    }

    public override void SurfaceCreated(ISurfaceHolder? p0)
    {
        base.SurfaceCreated(p0);
        SurfaceReady = true;
    }
}