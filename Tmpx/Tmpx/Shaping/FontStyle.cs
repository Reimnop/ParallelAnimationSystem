namespace Tmpx.Shaping;

[Flags]
public enum FontStyle : byte
{
    Regular = 0,
    Bold    = 1 << 0,
    Italic  = 1 << 1
}