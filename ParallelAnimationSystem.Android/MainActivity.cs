using System.Diagnostics;
using Android.Content;
using Android.Content.PM;
using Org.Libsdl.App;

[assembly: Application(
    HardwareAccelerated = false,
    ResizeableActivity = true,
    Theme = "@android:style/Theme.Black.NoTitleBar"
)]
namespace ParallelAnimationSystem.Android;

[Activity(
    Label = "@string/app_name",
    ConfigurationChanges = DefaultConfigChanges,
    LaunchMode = DefaultLaunchMode,
    ScreenOrientation = ScreenOrientation.Landscape,
    MainLauncher = true)]
public class MainActivity : SDLActivity
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
        var appSettings = new AndroidAppSettings(
            1, 4,
            (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            true);
        
        var startup = new AndroidStartup(appSettings);
        using var app = startup.InitializeApp();
        
        using var audioStream = typeof(MainActivity).Assembly.GetManifestResourceStream("ParallelAnimationSystem.Android.Beatmap.level.ogg");
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