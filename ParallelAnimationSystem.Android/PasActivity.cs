using System.Diagnostics;
using Android.Content.PM;
using Android.Views;
using MattiasCibien.Extensions.Logging.Logcat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGLES;
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
            InitialSize = new Vector2i(1366, 768),
            SwapInterval = 1,
            WorkerCount = 6,
            Seed = (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds(),
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
            builder.UseWindowManager<AndroidWindowManager>();
            builder.UseMediaProvider<AndroidMediaProvider>();
            builder.UseOpenGLESRenderer();
        });
        
        // Initialize PAS
        using var _ = services.InitializePAS(out var beatmapRunner, out var renderer);

        var appShutdown = false;
        
        var appThread = new Thread(() =>
        {
            // Create audio player
            using var audioPlayer = AudioPlayer.Load(audioData);
            
            // Start playback
            audioPlayer.Play();
            var baseFrequency = audioPlayer.Frequency;
            var speed = 1.0f;
            audioPlayer.Frequency = baseFrequency * speed;

            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            // ReSharper disable once AccessToModifiedClosure
            while (!appShutdown)
                if (!beatmapRunner.ProcessFrame((float) audioPlayer.Position))
                    Thread.Yield();
            
            // Stop playback
            audioPlayer.Stop();
        });
        
        appThread.Start();
        
        // Enter the render loop
        while (!renderer.Window.ShouldClose)
            if (!renderer.ProcessFrame())
                Thread.Yield();
        
        // When renderer exits, we'll shut down the services
        appShutdown = true;
        
        // Wait for the app thread to finish
        appThread.Join();
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