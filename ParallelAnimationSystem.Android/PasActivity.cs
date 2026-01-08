using System.Diagnostics;
using Android.Content.PM;
using Android.Views;
using ParallelAnimationSystem.Core;
using Activity = Android.App.Activity;
using Uri = Android.Net.Uri;

namespace ParallelAnimationSystem.Android;

[Activity(
    Label = "@string/app_name",
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    HardwareAccelerated = false,
    ConfigurationChanges = DefaultConfigChanges,
    LaunchMode = DefaultLaunchMode,
    ScreenOrientation = ScreenOrientation.Landscape)]
public class PasActivity : Activity
{
    private const ConfigChanges DefaultConfigChanges = ConfigChanges.Keyboard
                                                           | ConfigChanges.KeyboardHidden
                                                           | ConfigChanges.Navigation
                                                           | ConfigChanges.Orientation
                                                           | ConfigChanges.ScreenLayout
                                                           | ConfigChanges.ScreenSize
                                                           | ConfigChanges.SmallestScreenSize
                                                           | ConfigChanges.Touchscreen
                                                           | ConfigChanges.UiMode;

    private const LaunchMode DefaultLaunchMode = LaunchMode.SingleInstance;

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
        
        var appSettings = new AndroidAppSettings(
            1, 6,
            (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            lockAspectRatio ? 16.0f / 9.0f : null,
            enablePostProcessing,
            enableTextRendering);
        
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
        AndroidAppSettings appSettings,
        Uri beatmapPath,
        BeatmapFormat beatmapFormat,
        Uri audioPath, 
        GraphicsSurfaceView surfaceView,
        ISurfaceHolder surfaceHolder)
    {
        var contentResolver = ContentResolver;
        Debug.Assert(contentResolver is not null);

        string beatmapData;
        using (var stream = contentResolver.OpenInputStream(beatmapPath))
        {
            if (stream is null)
                throw new Exception("Failed to open beatmap stream");
            
            using var reader = new StreamReader(stream);
            beatmapData = reader.ReadToEnd();
        }
        
        using var audioStream = new MemoryStream();
        
        using (var stream = contentResolver.OpenInputStream(audioPath))
        {
            if (stream is null)
                throw new Exception("Failed to open audio stream");
            
            stream.CopyTo(audioStream);
        }

        audioStream.Seek(0L, SeekOrigin.Begin);
        
        using var audioPlayer = AudioPlayer.Load(audioStream);
        
        var startup = new AndroidStartup(appSettings, beatmapData, beatmapFormat, surfaceView, surfaceHolder);
        using var app = startup.InitializeApp();
            
        var beatmapRunner = app.BeatmapRunner;
        var renderer = app.Renderer;
                
        var logicThread = new Thread(() =>
        {
            // ReSharper disable once AccessToModifiedClosure
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!renderer.Window.ShouldClose)
                // ReSharper disable once AccessToDisposedClosure
                if (!beatmapRunner.ProcessFrame((float) audioPlayer.Position))
                    Thread.Yield();
        });
                
        logicThread.Start();
        
        audioPlayer.Play();

        while (!renderer.Window.ShouldClose)
        {
            if (!renderer.ProcessFrame())
                Thread.Yield();
        }
        
        logicThread.Join();
        
        audioPlayer.Stop();
    }
}