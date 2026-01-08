using System.Text;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using ParallelAnimationSystem.Core;

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
    
    private bool shouldExit = false;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        System.Diagnostics.Debug.Assert(Intent is not null);
        System.Diagnostics.Debug.Assert(Window is not null);
        
        Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        
        // Get settings from intent extras
        var lockAspectRatio = Intent.GetBooleanExtra("lockAspectRatio", true);
        var enablePostProcessing = Intent.GetBooleanExtra("postProcessing", true);
        var enableTextRendering = Intent.GetBooleanExtra("textRendering", true);
        
        var appSettings = new AndroidAppSettings(
            1, 6,
            (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            lockAspectRatio ? 16.0f / 9.0f : null,
            enablePostProcessing,
            enableTextRendering);
        
        // Load beatmap data
        var (beatmapData, audioData) = BeatmapDataTransfer.GetBeatmapData() ?? throw new Exception("Failed to load beatmap data");
        
        // Load audio
        if (audioData is null)
            throw new Exception("Failed to load audio data");
        
        // Load beatmap
        var beatmapFormat = (BeatmapFormat) Intent.GetIntExtra("beatmapFormat", 0);
        var beatmapString = Encoding.UTF8.GetString(beatmapData);
        
        // Create graphics surface
        var surface = new GraphicsSurface(this);
        
        // Start the app
        surface.SurfaceCreatedCallback = holder =>
        {
            var thread = new Thread(() => RunApp(appSettings, beatmapString, beatmapFormat, audioData, holder));
            thread.Start();
        };
        
        surface.SurfaceDestroyedCallback = _ =>
        {
            shouldExit = true;
            
            // Navigate back to the previous activity
            RunOnUiThread(() => StartActivity(new Intent(this, typeof(MainActivity))));
        };
        
        SetContentView(surface);
    }

    private void RunApp(AndroidAppSettings appSettings, string beatmapString, BeatmapFormat beatmapFormat, byte[] audioData, ISurfaceHolder holder)
    {
        using var audioStream = new MemoryStream(audioData);
        if (audioStream is null)
            throw new Exception("Failed to load audio stream");
        
        using var audioPlayer = AudioPlayer.Load(audioStream);
        
        var startup = new AndroidStartup(appSettings, beatmapString, beatmapFormat, holder);
        using var app = startup.InitializeApp();
            
        var beatmapRunner = app.BeatmapRunner;
        var renderer = app.Renderer;
                
        var logicThread = new Thread(() =>
        {
            // ReSharper disable once AccessToModifiedClosure
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!shouldExit)
                // ReSharper disable once AccessToDisposedClosure
                if (!beatmapRunner.ProcessFrame((float) audioPlayer.Position))
                    Thread.Yield();
        });
                
        logicThread.Start();
        
        audioPlayer.Play();

        while (!shouldExit)
        {
            if (renderer.Window.ShouldClose)
                shouldExit = true;
            
            if (!renderer.ProcessFrame())
                Thread.Yield();
        }
        
        logicThread.Join();
        
        audioPlayer.Stop();
    }
}