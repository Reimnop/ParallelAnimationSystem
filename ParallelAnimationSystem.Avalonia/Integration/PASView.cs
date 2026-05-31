using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Avalonia.Integration;

public class PASView : OpenGlControlBase
{
    private class RenderContext
    {
        public required RenderQueue RenderQueue { get; init; }
        public required IServiceScope Scope { get; init; }
        public required PASWindow Window { get; init; }
        public required IRenderer Renderer { get; init; }
    }
    
    private RenderContext? renderContext;
    
    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);
        
        if (DataContext is not PASViewModel vm)
            throw new InvalidOperationException($"DataContext must be of type {nameof(PASViewModel)}");
        
        if (renderContext is not null)
            throw new InvalidOperationException("RenderContext is already initialized");
        
        var sp = vm.InternalServiceProvider;
        
        var renderQueue = (RenderQueue)sp.GetRequiredService<IRenderQueue>();
        var windowHolder = sp.GetRequiredService<PASWindowHolder>();
        
        if (windowHolder.Window is not null)
            throw new InvalidOperationException("Window is already initialized");
        
        var window = new PASWindow(this, gl);
        windowHolder.Window = window;
        
        var scope = sp.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();
        
        renderContext = new RenderContext
        {
            RenderQueue = renderQueue,
            Scope = scope,
            Window = window,
            Renderer = renderer
        };
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        base.OnOpenGlDeinit(gl);
        
        if (DataContext is not PASViewModel vm)
            throw new InvalidOperationException($"DataContext must be of type {nameof(PASViewModel)}");
        
        var sp = vm.InternalServiceProvider;
        var windowHolder = sp.GetRequiredService<PASWindowHolder>();
        windowHolder.Window = null;
        
        if (renderContext is null)
            return;
        
        renderContext.Scope.Dispose();
        renderContext = null;
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (DataContext is not PASViewModel vm)
            throw new InvalidOperationException($"DataContext must be of type {nameof(PASViewModel)}");
        
        if (renderContext is null)
            return;

        vm.ProcessFrame();

        renderContext.Window.TargetFramebufferHandle = fb;
        renderContext.RenderQueue.ProcessFrame(renderContext.Renderer);
        
        RequestNextFrameRendering(); 
    }
}