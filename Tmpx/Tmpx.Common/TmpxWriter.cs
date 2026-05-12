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
        obj["spriteMap"] = WriteSpriteMap(font.SpriteMap);
        obj["glyphMap"] = WriteGlyphMap(font.GlyphMap);
        obj["kerning"] = WriteKerning(font.Kerning);
        
        obj["curves"] = WriteBuffer(font.Curves);
        obj["curveIndices"] = WriteBuffer(font.CurveIndices);
        obj["bandEntries"] = WriteBuffer(font.BandEntries);
        obj["shapeEntries"] = WriteBuffer(font.ShapeEntries);
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
    
    private static SsbfArray WriteSpriteMap(Dictionary<string, Sprite> sprites)
    {
        var arr = new SsbfArray();
        foreach (var kvp in sprites)
        {
            var entryObj = new SsbfObject
            {
                ["name"] = kvp.Key,
                ["advanceWidth"] = kvp.Value.AdvanceWidth,
                ["ascender"] = kvp.Value.Ascender,
                ["descender"] = kvp.Value.Descender,
                ["shapeEntryIndices"] = WriteShapeEntryIndices(kvp.Value.ShapeEntryIndices)
            };
            arr.Add(entryObj);
        }
        return arr;
    }
    
    private static SsbfArray WriteGlyphMap(Dictionary<char, Glyph> glyphMap)
    {
        var arr = new SsbfArray();
        foreach (var kvp in glyphMap)
        {
            var entryObj = new SsbfObject
            {
                ["codepoint"] = (ushort)kvp.Key,
                ["advanceWidth"] = kvp.Value.AdvanceWidth,
                ["shapeEntryIndex"] = kvp.Value.ShapeEntryIndex
            };
            arr.Add(entryObj);
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

    private static SsbfArray WriteShapeEntryIndices(List<int> shapeEntryIndices)
    {
        var arr = new SsbfArray();
        foreach (var index in shapeEntryIndices)
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