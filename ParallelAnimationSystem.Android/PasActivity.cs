using System.Text;
using Android.Content;
using Android.Content.PM;
using Org.Libsdl.App;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Android;

[Activity(
    Label = "@string/app_name",
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    HardwareAccelerated = false,
    ConfigurationChanges = DefaultConfigChanges,
    LaunchMode = DefaultLaunchMode,
    ScreenOrientation = ScreenOrientation.Landscape)]
public class PasActivity : SDLActivity
{
    public static AndroidSurface? Surface => MSurface as AndroidSurface;
    public static bool SurfaceReady => Surface?.SurfaceReady ?? false;
    
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
    
    // This can be treated as our program's entry point on Android
    protected override void Main()
    {
        var enablePostProcessing = Intent!.GetBooleanExtra("postProcessing", true);
        var enableTextRendering = Intent!.GetBooleanExtra("textRendering", true);
        
        var appSettings = new AndroidAppSettings(
            1, 6,
            (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            enablePostProcessing,
            enableTextRendering);
        
        var (beatmapData, audioData) = BeatmapDataTransfer.GetBeatmapData() ?? throw new Exception("Failed to load beatmap data");
        
        var beatmapFormat = (BeatmapFormat) Intent.GetIntExtra("beatmapFormat", 0);
        var beatmapString = Encoding.UTF8.GetString(beatmapData);
        
        var startup = new AndroidStartup(appSettings, beatmapString, beatmapFormat);
        using var app = startup.InitializeApp();
        
        if (audioData is null)
            throw new Exception("Failed to load audio data");
        
        using var audioStream = new MemoryStream(audioData);
        if (audioStream is null)
            throw new Exception("Failed to load audio stream");
        
        using var audioPlayer = AudioPlayer.Load(audioStream);
        
        var beatmapRunner = app.BeatmapRunner;
        var renderer = app.Renderer;
        
        var appShutdown = false;
        var appThread = new Thread(() =>
        {
            // ReSharper disable once AccessToModifiedClosure
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!appShutdown)
                // ReSharper disable once AccessToDisposedClosure
                if (!beatmapRunner.ProcessFrame(audioPlayer.Position))
                    Thread.Yield();
        });
        appThread.Start();
        
        audioPlayer.Play();
        
        // Enter the render loop
        while (!renderer.Window.ShouldClose)
            if (!SurfaceReady || !renderer.ProcessFrame())
                Thread.Yield();
        
        // When renderer exits, we'll shut down the services
        appShutdown = true;
        
        // Wait for the app thread to finish
        appThread.Join();
    }

    protected override SDLSurface CreateSDLSurface(Context? p0)
        => new AndroidSurface(p0);

    protected override string[] GetLibraries()
        => ["SDL3"];
}