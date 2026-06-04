using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Microsoft.Extensions.DependencyInjection;
using OpenTK;
using OpenTK.Graphics;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Avalonia.Integration;

public class PASView : OpenGlControlBase
{
    private class BindingsContext(GlInterface gl) : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
            => gl.GetProcAddress(procName);
    }
    
    private class RenderContext
    {
        public required RenderQueue RenderQueue { get; init; }
        public required IServiceScope Scope { get; init; }
        public required PASSurface Surface { get; init; }
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
        
        var sp = vm.ServiceProvider;
        
        var renderQueue = sp.GetRequiredService<RenderQueue>();
        var viewHolder = sp.GetRequiredService<PASViewHolder>();
        
        if (viewHolder.View is not null)
            throw new InvalidOperationException("Another view is already initialized");

        viewHolder.View = this;
        
        GLLoader.LoadBindings(new BindingsContext(gl));
        
        var scope = sp.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();
        var surface = (PASSurface)scope.ServiceProvider.GetRequiredService<IOpenGLSurface>();
        
        renderContext = new RenderContext
        {
            RenderQueue = renderQueue,
            Scope = scope,
            Surface = surface,
            Renderer = renderer
        };
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        base.OnOpenGlDeinit(gl);
        
        if (DataContext is not PASViewModel vm)
            throw new InvalidOperationException($"DataContext must be of type {nameof(PASViewModel)}");
        
        var sp = vm.ServiceProvider;
        var viewHolder = sp.GetRequiredService<PASViewHolder>();
        viewHolder.View = null;
        
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

        var surface = renderContext.Surface;
        var renderQueue = renderContext.RenderQueue;
        
        surface.TargetFramebufferHandle = fb;
        
        vm.ProcessFrame();
        renderQueue.FinishFrame();
        renderQueue.FlushFrame(renderContext.Renderer);
        
        RequestNextFrameRendering(); 
    }
}