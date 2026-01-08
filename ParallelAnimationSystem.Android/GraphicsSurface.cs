using System.Diagnostics;
using Android.Content;
using Android.Graphics;
using Android.Views;

namespace ParallelAnimationSystem.Android;

public delegate void SurfaceCreatedCallback(ISurfaceHolder holder);
public delegate void SurfaceDestroyedCallback(ISurfaceHolder holder);
public delegate void SurfaceChangedCallback(ISurfaceHolder holder, Format format, int width, int height);

public class GraphicsSurface : SurfaceView, ISurfaceHolderCallback
{
    public SurfaceCreatedCallback? SurfaceCreatedCallback { get; set; }
    public SurfaceDestroyedCallback? SurfaceDestroyedCallback { get; set; }
    public SurfaceChangedCallback? SurfaceChangedCallback { get; set; }
    
    public GraphicsSurface(Context? p0) : base(p0)
    {
        var holder = Holder;
        Debug.Assert(holder is not null);
        
        holder.AddCallback(this);
    }

    public void SurfaceCreated(ISurfaceHolder holder)
        => SurfaceCreatedCallback?.Invoke(holder);

    public void SurfaceDestroyed(ISurfaceHolder holder)
        => SurfaceDestroyedCallback?.Invoke(holder);
    
    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        => SurfaceChangedCallback?.Invoke(holder, format, width, height);
}