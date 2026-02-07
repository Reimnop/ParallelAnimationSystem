using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Data;

public record struct Identifier(ulong Value)
{
    public Identifier Combine(Identifier other) 
        => Combine(this, other);
    
    public static Identifier Combine(Identifier a, Identifier b) 
        => NumberUtil.Mix(a.Value, b.Value);

    public static Identifier FromString(string str)
        => NumberUtil.ComputeHash(str);
    
    public static implicit operator Identifier(ulong value) => new(value);
    public static implicit operator Identifier(string str) => FromString(str);
    public static implicit operator ulong(Identifier id) => id.Value;

    public override string ToString()
        => Value.ToString("X16");
}