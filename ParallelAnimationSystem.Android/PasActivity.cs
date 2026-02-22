using System.Diagnostics;
using Android.Content.PM;
using Android.Views;
using MattiasCibien.Extensions.Logging.Logcat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
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
public class PasActivity : Activity
{
    private const ConfigChanges DefaultConfigChanges = (ConfigChanges) ~0;
    private const LaunchMode DefaultLaunchMode = LaunchMode.SingleTask;

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

        var appSettings = new AppSettings
        {
            Seed = NumberUtil.SplitMix64((ulong) DateTimeOffset.Now.ToUnixTimeSeconds()),
            AspectRatio = lockAspectRatio ? 16.0f / 9.0f : null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };
        
        // Create graphics surface
        var surfaceView = new GraphicsSurfaceView(this);
        
        // Start the app
        surfaceView.SurfaceCreatedCallback = surfaceHolder =>
        {
            var thread = new Thread(() => 
                RunApp(appSettings, beatmapPath, beatmapFormat, audioPath, surfaceView, surfaceHolder));
            thread.Start();
        };
        
        SetContentView(surfaceView);
    }

    private void RunApp(
        AppSettings appSettings,
        Uri beatmapPath,
        BeatmapFormat beatmapFormat,
        Uri audioPath, 
        GraphicsSurfaceView surfaceView,
        ISurfaceHolder surfaceHolder)
    {
        var beatmapData = ReadBeatmapData(beatmapPath);
        var audioData = ReadAudioData(audioPath);
        
        var services = new ServiceCollection();
        
        // Register contexts
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
        
        // Register PAS services
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindow<AndroidWindow>();
            builder.UseMediaProvider<AndroidMediaProvider>();
            builder.UseOpenGLESRenderer();
        });
        
        // Initialize PAS services
        var serviceProvider = services.BuildServiceProvider();

        var appCore = serviceProvider.InitializeAppCore();
        var renderer = serviceProvider.InitializeRenderer();
        
        var window = serviceProvider.GetRequiredService<IWindow>();
        
        // Get a draw list
        var renderingFactory = serviceProvider.GetRequiredService<IRenderingFactory>();
        var drawList = renderingFactory.CreateDrawList();
        
        // Initialize audio player
        using var audioPlayer = AudioPlayer.Load(audioData);
        audioPlayer.Play();
        
        // Enter main loop
        while (!window.ShouldClose)
        {
            window.PollEvents();
            
            // Process a frame
            appCore.ProcessFrame((float) audioPlayer.Position, drawList);
            renderer.ProcessFrame(drawList);
            
            // Clear the draw list for the next frame
            drawList.Clear();
        }
        
        audioPlayer.Stop();
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