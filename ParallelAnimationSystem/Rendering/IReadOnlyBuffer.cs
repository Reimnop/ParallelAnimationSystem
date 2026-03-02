namespace ParallelAnimationSystem.Rendering;

public interface IReadOnlyBuffer<T> where T : unmanaged
{
    ReadOnlySpan<T> Data { get; }
    ReadOnlySpan<byte> DataAsBytes { get; }
    
    int Length { get; }
    int LengthInBytes { get; }
}