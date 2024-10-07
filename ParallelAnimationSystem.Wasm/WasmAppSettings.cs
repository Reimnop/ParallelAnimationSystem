namespace ParallelAnimationSystem.Wasm;

public sealed class WasmAppSettings(ulong seed, bool enablePostProcessing, bool enableTextRendering) : IAppSettings
{
    public int SwapInterval => 0; // Not supported in WebAssembly
    public int WorkerCount => -1; // Not supported in WebAssembly
    public ulong Seed { get; } = seed;
    public float? AspectRatio => null; // Just handle this in CSS
    public bool EnablePostProcessing { get; } = enablePostProcessing;
    public bool EnableTextRendering { get; } = enableTextRendering;
}