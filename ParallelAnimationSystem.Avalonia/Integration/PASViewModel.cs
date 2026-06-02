using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Avalonia.ViewModels;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Windowing;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia.Integration;

[ViewModelView(typeof(PASView))]
public class PASViewModel : ViewModelBase, IDisposable
{
    public Func<float>? TickCallback { get; set; }

    public IServiceProvider InternalServiceProvider { get; }
    public BeatmapService BeatmapService { get; }

    private readonly AppDirector director;

    public PASViewModel()
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

        services.AddSingleton<PASWindowHolder>();
        services.AddScoped<IWindow>(x =>
        {
            var holder = x.GetRequiredService<PASWindowHolder>();
            if (holder.Window is null)
                throw new NullReferenceException("Window is null");
            return holder.Window;
        });
        
        InternalServiceProvider = services.BuildServiceProvider();
        
        var directorScope = InternalServiceProvider.CreateScope();
        BeatmapService = directorScope.ServiceProvider.GetRequiredService<BeatmapService>();
        director = directorScope.ServiceProvider.GetRequiredService<AppDirector>();
    }

    public void ProcessFrame()
    {
        director.ProcessFrame(TickCallback?.Invoke() ?? 0f);
    }

    public void Dispose()
    {
        if (InternalServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}