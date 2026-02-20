using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public class RandomizableKeyframeInteropAdapter<T> : IKeyframeInteropAdapter<RandomizableKeyframe<T>> where T : unmanaged
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Buffer
    {
        public float Time;
        public Ease Ease;
        public T Value;
        public RandomMode RandomMode;
        public T RandomValue;
        public float RandomInterval;
        public bool IsRelative;
    }
    
    public int Size => Unsafe.SizeOf<Buffer>();
    
    public unsafe void Write(RandomizableKeyframe<T> keyframe, IntPtr bufferPtr)
    {
        var data = new Buffer
        {
            Time = keyframe.Time,
            Ease = keyframe.Ease,
            Value = keyframe.Value,
            RandomMode = keyframe.RandomMode,
            RandomValue = keyframe.RandomValue,
            RandomInterval = keyframe.RandomInterval,
            IsRelative = keyframe.IsRelative
        };
        Unsafe.Write(bufferPtr.ToPointer(), data);
    }

    public unsafe RandomizableKeyframe<T> Read(IntPtr bufferPtr)
    {
        var buffer = Unsafe.Read<Buffer>(bufferPtr.ToPointer());
        return new RandomizableKeyframe<T>(
            buffer.Time, buffer.Ease, buffer.Value,
            buffer.RandomMode, buffer.RandomValue, buffer.RandomInterval,
            buffer.IsRelative);
    }
}