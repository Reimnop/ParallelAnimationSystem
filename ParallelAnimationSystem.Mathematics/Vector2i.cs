using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Mathematics;

[StructLayout(LayoutKind.Sequential)]
public struct Vector2i(int x, int y) : IEquatable<Vector2i>
{
    public static Vector2i Zero => new(0, 0);
    public static Vector2i One => new(1, 1);
    
    public int X
    {
        get => x;
        set => x = value;
    }
    
    public int Y
    {
        get => y;
        set => y = value;
    }

    public Vector2i() : this(0, 0)
    {
    }
    
    public override string ToString() => $"({X}, {Y})";
    
    public static Vector2i operator +(Vector2i a, Vector2i b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2i operator -(Vector2i a, Vector2i b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2i operator *(Vector2i a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2i operator /(Vector2i a, int scalar) => new(a.X / scalar, a.Y / scalar);
    
    public static Vector2i operator -(Vector2i a) => new(-a.X, -a.Y);
    
    public static bool operator ==(Vector2i a, Vector2i b) => a.Equals(b);
    public static bool operator !=(Vector2i a, Vector2i b) => !a.Equals(b);

    public bool Equals(Vector2i other)
        => X == other.X && Y == other.Y;
    
    public override int GetHashCode() => HashCode.Combine(X, Y);
    
    public override bool Equals(object? obj)
    {
        if (obj is not Vector2i other)
            return false;
        
        return X == other.X && Y == other.Y;
    }
}