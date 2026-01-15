using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;

namespace ParallelAnimationSystem.Desktop;

public static class DesktopStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        bool vsync,
        int workerCount,
        long seed,
        float speed,
        RenderingBackend backend,
        bool lockAspectRatio,
        bool enablePostProcessing,
        bool enableTextRendering)
    {
        var appSettings = new AppSettings
        {
            InitialSize = new Vector2i(1366, 768),
            SwapInterval = vsync ? 1 : 0,
            WorkerCount = workerCount,
            Seed = seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) seed,
            AspectRatio = lockAspectRatio ? 16.0f / 9.0f : null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };

        var services = new ServiceCollection();

        services.AddSingleton(new MediaContext
        {
            BeatmapPath = beatmapPath
        });
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindowManager<DesktopWindowManager>();
            builder.UseMediaProvider<DesktopMediaProvider>();
            switch (backend)
            {
                case RenderingBackend.OpenGL:
                    builder.UseOpenGLRenderer();
                    break;
                case RenderingBackend.OpenGLES:
                    builder.UseOpenGLESRenderer();
                    break;
            }
        });

        using var _ = services.InitializePAS(out var beatmapRunner, out var renderer);
        
        var appShutdown = false;
        var appThread = new Thread(() =>
        {
            // Load audio in app thread
            using var audioPlayer = AudioPlayer.Load(audioPath);
            var baseFrequency = audioPlayer.Frequency;
            audioPlayer.Frequency = baseFrequency * speed;
            
            // Start playback
            audioPlayer.Play();
            
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
}