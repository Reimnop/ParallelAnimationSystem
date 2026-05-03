using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Rendering;

public class Buffer<T>(int capacity = 1024): IReadOnlyBuffer<T> where T : unmanaged
{    
    public ReadOnlySpan<T> Data => data.AsSpan(0, length);
    public ReadOnlySpan<byte> DataAsBytes => MemoryMarshal.AsBytes(Data);

    /// <summary>
    /// Returns a writable span into the buffer's currently-populated region. Useful for in-place
    /// patches after an <see cref="Append(ReadOnlySpan{T})"/> — e.g. fixing up indices that were
    /// stored in font-local form before being concatenated into a global buffer.
    /// </summary>
    public Span<T> AsWritableSpan(int start, int length)
        => data.AsSpan(start, length);
    
    public int Length => length;
    public int LengthInBytes => length * Unsafe.SizeOf<T>();

    private T[] data = new T[capacity];

    private int length;
    
    public void EnsureSize(int size)
    {
        if (size < data.Length)
            return;
        
        var newSize = data.Length * 2;
        newSize = Math.Max(newSize, size);
        Array.Resize(ref data, newSize);
    }

    public void Append(T data)
    {
        var start = length;

        // Resize buffer if necessary
        EnsureSize(start + 1);
        
        // Copy data
        this.data[start] = data;

        // Update size
        length++;
    }
    
    public void Append(ReadOnlySpan<T> data)
    {
        var start = length;

        // Resize buffer if necessary
        EnsureSize(start + data.Length);
        
        // Copy data
        data.CopyTo(this.data.AsSpan(start));
        
        // Update size
        length += data.Length;
    }
    
    public void Clear()
    {
        length = 0;
    }
}