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

[ViewModelView<PASView>]
public class PASViewModel : ViewModelBase, IDisposable
{
    public Func<float>? TickCallback { get; set; }

    public IServiceProvider ServiceProvider { get; }

    public BeatmapService BeatmapService { get; }
    public RandomSeedService RandomSeedService { get; }
    
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
        
        ServiceProvider = services.BuildServiceProvider();
        
        var directorScope = ServiceProvider.CreateScope();
        var directorSp = directorScope.ServiceProvider;
        
        director = directorSp.GetRequiredService<AppDirector>();
        BeatmapService = directorSp.GetRequiredService<BeatmapService>();
        RandomSeedService = directorSp.GetRequiredService<RandomSeedService>();
    }

    public void ProcessFrame()
    {
        director.ProcessFrame(TickCallback?.Invoke() ?? 0f);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}