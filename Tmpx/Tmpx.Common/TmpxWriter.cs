using System.Runtime.InteropServices;
using SimpleStructuredBinaryFormat;

namespace Tmpx.Common;

public static class TmpxWriter
{
    public static SsbfObject Write(Font font)
    {
        var obj = new SsbfObject();
        obj["familyName"] = font.FamilyName;
        obj["styleName"] = font.StyleName;
        obj["metrics"] = WriteMetrics(font.Metrics);
        obj["glyphMap"] = WriteGlyphMap(font.GlyphMap);
        obj["kerning"] = WriteKerning(font.Kerning);
        obj["sprites"] = WriteSprites(font.Sprites);
        obj["glyphs"] = WriteBuffer(font.Glyphs);
        obj["curves"] = WriteBuffer(font.Curves);
        obj["curveIndices"] = WriteBuffer(font.CurveIndices);
        obj["bandEntries"] = WriteBuffer(font.BandEntries);
        return obj;
    }

    private static SsbfObject WriteMetrics(FontMetrics metrics)
    {
        var obj = new SsbfObject
        {
            ["lineHeight"] = metrics.LineHeight,
            ["ascender"] = metrics.Ascender,
            ["descender"] = metrics.Descender
        };
        return obj;
    }
    
    private static SsbfArray WriteGlyphMap(Dictionary<char, int> glyphMap)
    {
        var arr = new SsbfArray();
        foreach (var kvp in glyphMap)
        {
            var entryArr = new SsbfArray
            {
                (ushort)kvp.Key,
                kvp.Value
            };
            arr.Add(entryArr);
        }
        return arr;
    }
    
    private static SsbfArray WriteKerning(Dictionary<(char, char), float> kerning)
    {
        var arr = new SsbfArray();
        foreach (var kvp in kerning)
        {
            var entryArr = new SsbfArray
            {
                (ushort)kvp.Key.Item1,
                (ushort)kvp.Key.Item2,
                kvp.Value
            };
            arr.Add(entryArr);
        }
        return arr;
    }
    
    private static SsbfArray WriteSprites(Dictionary<string, Sprite> sprites)
    {
        var arr = new SsbfArray();
        foreach (var kvp in sprites)
        {
            var entryObj = new SsbfObject
            {
                ["name"] = kvp.Key,
                ["advanceWidth"] = kvp.Value.AdvanceWidth,
                ["glyphIndices"] = WriteGlyphIndices(kvp.Value.GlyphIndices)
            };
            arr.Add(entryObj);
        }
        return arr;
    }

    private static SsbfArray WriteGlyphIndices(List<int> glyphIndices)
    {
        var arr = new SsbfArray();
        foreach (var index in glyphIndices)
            arr.Add(index);
        return arr;
    }

    private static SsbfByteArray WriteBuffer<T>(T[] items) where T : unmanaged
    {
        using var ms = new MemoryStream();
        var span = items.AsSpan();
        var byteSpan = MemoryMarshal.AsBytes(span);
        ms.Write(byteSpan);
        return new SsbfByteArray(ms.ToArray());
    }
}