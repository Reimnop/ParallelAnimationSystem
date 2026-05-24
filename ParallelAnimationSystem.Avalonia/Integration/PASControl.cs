using System;
using System.Numerics;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Avalonia.Integration;

public class PASControl : OpenGlControlBase, IOpenGLWindow, IDisposable
{
    public static readonly StyledProperty<Func<float>?> GetTimeCallbackProperty =
        AvaloniaProperty.Register<PASControl, Func<float>?>(nameof(GetTimeCallback));

    public Func<float>? GetTimeCallback
    {
        get => GetValue(GetTimeCallbackProperty);
        set => SetValue(GetTimeCallbackProperty, value);
    }
    
    public IServiceProvider InternalServiceProvider => directorScope.ServiceProvider;
    
    private readonly IServiceProvider sp;
    private readonly IServiceScope directorScope;
    private readonly AppDirector director;

    private IServiceScope? renderScope;
    private IRenderer? renderer;
    private RenderQueue? renderQueue;
    private GlInterface? gl;
    private int targetFramebuffer;
    
    public PASControl()
    {
        var appSettings = new AppSettings
        {
            AspectRatio = null,
            EnablePostProcessing = true,
            EnableTextRendering = true,
        };
        
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        
        services.AddPAS()
            .UseAppSettings(appSettings)
            .UseRenderQueue<RenderQueue>()
            .UseOpenGLESRenderer();
        
        services.AddScoped<IWindow>(_ => this);
        
        sp = services.BuildServiceProvider();
        
        directorScope = sp.CreateScope();
        director = directorScope.ServiceProvider.GetRequiredService<AppDirector>();
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        this.gl = gl;
        
        renderScope = sp.CreateScope();
        renderer = renderScope.ServiceProvider.GetRequiredService<IRenderer>();
        renderQueue = (RenderQueue)renderScope.ServiceProvider.GetRequiredService<IRenderQueue>();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        base.OnOpenGlDeinit(gl);
        
        renderScope?.Dispose();
        renderScope = null;
        renderer = null;
        renderQueue = null;
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        targetFramebuffer = fb;
        director.ProcessFrame(GetTimeCallback?.Invoke() ?? 0f);
        renderQueue!.ProcessFrame(renderer!);
        
        RequestNextFrameRendering(); 
    }

    public void Dispose()
    {
        if (sp is IDisposable disposable)
            disposable.Dispose();
    }

    public Vector2i FramebufferSize
    {
        get
        {
            var dipSize = Bounds.Size;
            var renderScaling = this.GetPresentationSource()?.RenderScaling ?? 1f;
            return new Vector2i(
                Math.Max(1, (int)(dipSize.Width * renderScaling)),
                Math.Max(1, (int)(dipSize.Height * renderScaling)));
        }
    }

    public bool ShouldClose => false;
    public bool IsContextCurrent => true;
    
    public void PollEvents()
    {
    }

    public void Close()
    {
    }
    
    public void MakeContextCurrent()
    {
    }

    public void Present(int framebuffer, Vector4 clearColor, Vector2i size, Vector2i offset)
    {
        var dstSize = FramebufferSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, targetFramebuffer);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Viewport(0, 0, dstSize.X, dstSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            0, 0, size.X, size.Y,
            offset.X, offset.Y, offset.X + size.X, offset.Y + size.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
    }

    public IntPtr GetProcAddress(string procName)
        => gl!.GetProcAddress(procName);
}