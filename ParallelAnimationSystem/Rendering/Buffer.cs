using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Rendering;

public class Buffer<T>(int capacity = 1024) where T : unmanaged
{    
    public ReadOnlySpan<T> Data => data.AsSpan(0, length);
    public ReadOnlySpan<byte> DataAsBytes => MemoryMarshal.AsBytes(Data);
    
    public int Length => length;
    public int LengthInBytes => length * Marshal.SizeOf<T>();

    private T[] data = new T[capacity];

    private int length;

    public void Append(T data)
    {
        var start = length;

        // Resize buffer if necessary
        if (start + 1 >= this.data.Length)
        {
            var newSize = this.data.Length * 2;
            Array.Resize(ref this.data, newSize);
        }
        
        // Copy data
        this.data[start] = data;

        // Update size
        length++;
    }
    
    public void Append(ReadOnlySpan<T> data)
    {
        var start = length;

        // Resize buffer if necessary
        if (start + data.Length >= this.data.Length)
        {
            var newSize = this.data.Length;
            while (start + data.Length >= newSize)
                newSize *= 2;

            Array.Resize(ref this.data, newSize);
        }
        
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