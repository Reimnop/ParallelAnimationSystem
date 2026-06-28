using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Avalonia.ViewModels;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Resources.Compressed;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia.Integration;

[ViewModelView<PASView>]
public class PASViewModel : ViewModelBase, IDisposable
{
    public Func<float>? TickCallback { get; set; }

    public IServiceProvider ServiceProvider { get; }

    public BeatmapService BeatmapService { get; }
    public RandomSeedService RandomSeedService { get; }

    public AppDirector Director { get; }

    public PASViewModel()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        
        services.AddPAS(x => x.AddCompressedResources())
            .UseOpenGLESRenderer();

        services.AddSingleton<PASViewHolder>();
        services.AddScoped<IOpenGLSurface, PASSurface>(x =>
        {
            var holder = x.GetRequiredService<PASViewHolder>();
            if (holder.View is null)
                throw new NullReferenceException("View is null");
            return new PASSurface(holder.View);
        });
        
        ServiceProvider = services.BuildServiceProvider();
        
        var directorScope = ServiceProvider.CreateScope();
        var directorSp = directorScope.ServiceProvider;
        
        Director = directorSp.GetRequiredService<AppDirector>();
        BeatmapService = directorSp.GetRequiredService<BeatmapService>();
        RandomSeedService = directorSp.GetRequiredService<RandomSeedService>();
    }

    public void ProcessFrame()
    {
        Director.PopulateRenderQueueDrawList(TickCallback?.Invoke() ?? 0f);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}