using System.Diagnostics;
using Android.Content.PM;
using Android.Views;
using MattiasCibien.Extensions.Logging.Logcat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Resources.Compressed;
using ParallelAnimationSystem.Util;
using Activity = Android.App.Activity;
using Uri = Android.Net.Uri;

namespace ParallelAnimationSystem.Android;

[Activity(
    Label = "@string/app_name",
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    HardwareAccelerated = false,
    AlwaysRetainTaskState = true,
    ConfigurationChanges = DefaultConfigChanges,
    LaunchMode = DefaultLaunchMode,
    ScreenOrientation = ScreenOrientation.Landscape)]
public class PASActivity : Activity
{
    private class AppContext
    {
        public required bool IsRunning { get; set; }
        public required bool IsRendering { get; set; }
        public required IServiceProvider ServiceProvider { get; set; }
        public required RenderQueue RenderQueue { get; set; }
    }
    
    private const ConfigChanges DefaultConfigChanges = (ConfigChanges) ~0;
    private const LaunchMode DefaultLaunchMode = LaunchMode.SingleTask;

    private AppContext appContext = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        Debug.Assert(Intent is not null);
        Debug.Assert(Window is not null);
        
        // Get settings from intent extras
        var lockAspectRatio = Intent.GetBooleanExtra("lockAspectRatio", true);
        var enablePostProcessing = Intent.GetBooleanExtra("postProcessing", true);
        var enableTextRendering = Intent.GetBooleanExtra("textRendering", true);
        var beatmapFormat = (BeatmapFormat) Intent.GetIntExtra("beatmapFormat", 0);
        
#pragma warning disable CA1422
        var beatmapPath = Intent.GetParcelableExtra("beatmapPath") as Uri ?? throw new Exception("Beatmap path not provided in intent extras");
        var audioPath = Intent.GetParcelableExtra("audioPath") as Uri ?? throw new Exception("Audio path not provided in intent extras");
#pragma warning restore CA1422
        
        // Create graphics surface
        var surfaceView = new GraphicsSurfaceView(this);
        
        // Start the app
        surfaceView.SurfaceCreatedCallback = surfaceHolder =>
        {
            var thread = new Thread(() =>
                InitializeApp(beatmapPath, beatmapFormat, audioPath, surfaceView, surfaceHolder, lockAspectRatio, enableTextRendering, enablePostProcessing));
            thread.Start();
        };

        SetContentView(surfaceView);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        appContext.IsRunning = false;
        appContext.IsRendering = false;
    }

    private void InitializeApp(
        Uri beatmapPath,
        BeatmapFormat beatmapFormat,
        Uri audioPath,
        GraphicsSurfaceView surfaceView,
        ISurfaceHolder surfaceHolder,
        bool lockAspectRatio,
        bool enableTextRendering,
        bool enablePostProcessing)
    {
        var beatmapData = ReadBeatmapData(beatmapPath);
        var audioData = ReadAudioData(audioPath);
        
        var services = new ServiceCollection();
        
        services.AddSingleton(new AndroidSurfaceContext
        {
            SurfaceView = surfaceView,
            SurfaceHolder = surfaceHolder
        });

        services.AddSingleton(new BeatmapContext
        {
            Data = beatmapData,
            Format = beatmapFormat
        });

        // Register logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddLogcat("ParallelAnimationSystem");
        });
        
        var surfaceSettings = new AndroidSurfaceSettings
        {
            LockAspectRatio = lockAspectRatio,
        };
        services.AddSingleton(surfaceSettings);

        services.AddScoped<IOpenGLSurface, AndroidSurface>();

        // Register PAS services
        services.AddPAS(x => x.AddCompressedResources())
            .UseOpenGLESRenderer();

        // Initialize PAS services
        using var serviceProvider = services.BuildServiceProvider();

        var directorScope = serviceProvider.CreateScope();

        var director = directorScope.ServiceProvider.GetRequiredService<AppDirector>();
        director.EnablePostProcessing = enablePostProcessing;
        director.EnableTextRendering = enableTextRendering;

        var randomSeedService = directorScope.ServiceProvider.GetRequiredService<RandomSeedService>();
        randomSeedService.Seed = NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());

        var beatmapService = directorScope.ServiceProvider.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapData, beatmapFormat);

        var renderQueue = serviceProvider.GetRequiredService<RenderQueue>();

        appContext = new AppContext
        {
            IsRunning = true,
            IsRendering = true,
            ServiceProvider = serviceProvider,
            RenderQueue = renderQueue,
        };

        // Run the render thread
        var renderThread = new Thread(RunRenderThread);
        renderThread.Start();

        using var audioPlayer = AudioPlayer.Load(audioData);
        audioPlayer.Play();

        while (appContext.IsRunning)
        {
            if (renderQueue.FreeFrameCount > 0)
            {
                director.PopulateRenderQueueDrawList((float) audioPlayer.Position);
                renderQueue.FinishFrame();
            }
            else
                Thread.Yield();
        }

        audioPlayer.Stop();
    }

    private void RunRenderThread()
    {
        while (appContext.IsRunning)
        {
            while (!appContext.IsRendering)
                Thread.Yield();

            using var scope = appContext.ServiceProvider.CreateScope();

            var renderQueue = appContext.RenderQueue;
            var renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();

            // Start the render loop
            while (appContext.IsRendering)
                if (renderQueue.QueuedFrameCount > 0)
                    renderQueue.FlushFrame(renderer);
                else
                    Thread.Yield();
        }
    }
    
    private string ReadBeatmapData(Uri beatmapPath)
    {
        var contentResolver = ContentResolver;
        Debug.Assert(contentResolver is not null);

        using var stream = contentResolver.OpenInputStream(beatmapPath);
        if (stream is null)
            throw new Exception("Failed to open beatmap stream");
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    private byte[] ReadAudioData(Uri audioPath)
    {
        var contentResolver = ContentResolver;
        Debug.Assert(contentResolver is not null);

        using var stream = contentResolver.OpenInputStream(audioPath);
        if (stream is null)
            throw new Exception("Failed to open audio stream");
        
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        
        return memoryStream.ToArray();
    }
}