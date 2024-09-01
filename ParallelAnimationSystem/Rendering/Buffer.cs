using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Rendering;

public class Buffer(int capacity = 1024)
{    
    public ReadOnlySpan<byte> Data => data.AsSpan(0, length);

    private byte[] data = new byte[capacity];

    private int length;

    public void Append<T>(T data) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        var offset = length;

        // Resize buffer if necessary
        if (offset + size > this.data.Length)
        {
            var newSize = this.data.Length;
            while (offset + size > newSize)
                newSize *= 2;

            Array.Resize(ref this.data, newSize);
        }
        
        // Copy data
        unsafe
        {
            fixed (byte* ptr = this.data)
                Unsafe.CopyBlock(ptr + offset, &data, (uint) size);
        }

        // Update size
        length += size;
    }
    
    public void Append<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>() * data.Length;
        var offset = length;

        // Resize buffer if necessary
        if (offset + size > this.data.Length)
        {
            var newSize = this.data.Length;
            while (offset + size > newSize)
                newSize *= 2;

            Array.Resize(ref this.data, newSize);
        }
        
        // Copy data
        unsafe
        {
            fixed (byte* ptr = this.data)
            fixed (T* src = data)
                Unsafe.CopyBlock(ptr + offset, src, (uint) size);
        }
        
        // Update size
        length += size;
    }
    
    public void Clear()
    {
        length = 0;
    }
}