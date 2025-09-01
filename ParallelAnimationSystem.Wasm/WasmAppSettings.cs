using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Wasm;

public sealed class WasmAppSettings(ulong seed, float backgroundOpacity, bool enablePostProcessing, bool enableTextRendering) : IAppSettings
{
    public Vector2i InitialSize { get; } = new(1366, 768);
    public int SwapInterval => 0; // Not supported in WebAssembly
    public int WorkerCount => -1; // Not supported in WebAssembly
    public ulong Seed { get; } = seed;
    public float? AspectRatio => null; // Just handle this in CSS
    public float BackgroundOpacity { get; } = backgroundOpacity;
    public bool EnablePostProcessing { get; } = enablePostProcessing;
    public bool EnableTextRendering { get; } = enableTextRendering;
}