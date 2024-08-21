using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Rendering;

public class Buffer(int capacity = 1024)
{    
    public ReadOnlySpan<byte> Data => data.AsSpan(0, length);

    private byte[] data = new byte[capacity];

    private int length;

    public unsafe void Append<T>(T data) where T : unmanaged
    {
        var length = sizeof(T);
        var offset = this.length;

        // Resize buffer if necessary
        if (offset + length > this.data.Length)
        {
            var newSize = this.data.Length;
            while (offset + length > newSize)
                newSize *= 2;

            Array.Resize(ref this.data, newSize);
        }
        
        // Copy data
        fixed (byte* ptr = this.data)
            Unsafe.CopyBlock(ptr + offset, &data, (uint) length);

        // Update size
        this.length += length;
    }
    
    public unsafe void Append<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        var length = sizeof(T) * data.Length;
        var offset = this.length;

        // Resize buffer if necessary
        if (offset + length > this.data.Length)
        {
            var newSize = this.data.Length;
            while (offset + length > newSize)
                newSize *= 2;

            Array.Resize(ref this.data, newSize);
        }
        
        // Copy data
        fixed (byte* ptr = this.data)
            fixed (T* src = data)
                Unsafe.CopyBlock(ptr + offset, src, (uint) length);
        
        // Update size
        this.length += length;
    }
    
    public void Clear()
    {
        length = 0;
    }
}