using System.Runtime.InteropServices;
using SimpleStructuredBinaryFormat;

namespace Tmpx.Common;

public static class TmpxReader
{
    public static Font Read(SsbfObject obj)
    {
        var familyName = obj["familyName"]?.Get<string>() ?? string.Empty;
        var styleName = obj["styleName"]?.Get<string>() ?? string.Empty;
        var metrics = ReadMetrics(obj["metrics"]?.Get<SsbfObject>() ?? new SsbfObject());
        var glyphMap = ReadGlyphMap(obj["glyphMap"]?.Get<SsbfArray>() ?? new SsbfArray());
        var kerning = ReadKerning(obj["kerning"]?.Get<SsbfArray>() ?? new SsbfArray());
        var sprites = ReadSprites(obj["sprites"]?.Get<SsbfArray>() ?? new SsbfArray());
        var glyphs = ReadBuffer<Glyph>(obj["glyphs"]?.Get<SsbfByteArray>() ?? new SsbfByteArray([]));
        var curves = ReadBuffer<QuadraticCurve>(obj["curves"]?.Get<SsbfByteArray>() ?? new SsbfByteArray([]));
        var curveIndices = ReadBuffer<int>(obj["curveIndices"]?.Get<SsbfByteArray>() ?? new SsbfByteArray([]));
        var bandEntries = ReadBuffer<BandEntry>(obj["bandEntries"]?.Get<SsbfByteArray>() ?? new SsbfByteArray([]));
        
        return new Font
        {
            FamilyName = familyName,
            StyleName = styleName,
            Metrics = metrics,
            GlyphMap = glyphMap,
            Kerning = kerning,
            Sprites = sprites,
            Glyphs = glyphs,
            Curves = curves,
            CurveIndices = curveIndices,
            BandEntries = bandEntries
        };
    }
    
    private static FontMetrics ReadMetrics(SsbfObject obj)
    {
        var lineHeight = obj["lineHeight"]?.Get<float>() ?? 0f;
        var ascender = obj["ascender"]?.Get<float>() ?? 0f;
        var descender = obj["descender"]?.Get<float>() ?? 0f;
        return new FontMetrics
        {
            LineHeight = lineHeight,
            Ascender = ascender,
            Descender = descender
        };
    }
    
    private static Dictionary<char, int> ReadGlyphMap(SsbfArray arr)
    {
        var glyphMap = new Dictionary<char, int>();
        foreach (var entry in arr)
        {
            var entryArr = entry?.Get<SsbfArray>() ?? new SsbfArray();
            var codepoint = (char)(entryArr.Count > 0 ? entryArr[0]?.Get<ushort>() ?? 0 : 0);
            var glyphIndex = entryArr.Count > 1 ? entryArr[1]?.Get<int>() ?? 0 : 0;
            glyphMap[codepoint] = glyphIndex;
        }
        return glyphMap;
    }
    
    private static Dictionary<(char, char), float> ReadKerning(SsbfArray arr)
    {
        var kerning = new Dictionary<(char, char), float>();
        foreach (var entry in arr)
        {
            var entryArr = entry?.Get<SsbfArray>() ?? new SsbfArray();
            var first = (char)(entryArr.Count > 0 ? entryArr[0]?.Get<ushort>() ?? 0 : 0);
            var second = (char)(entryArr.Count > 1 ? entryArr[1]?.Get<ushort>() ?? 0 : 0);
            var value = entryArr.Count > 2 ? entryArr[2]?.Get<float>() ?? 0f : 0f;
            kerning[(first, second)] = value;
        }
        return kerning;
    }
    
    private static Dictionary<string, Sprite> ReadSprites(SsbfArray arr)
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (var entry in arr)
        {
            var entryObj = entry?.Get<SsbfObject>() ?? new SsbfObject();
            var name = entryObj["name"]?.Get<string>() ?? string.Empty;
            var advanceWidth = entryObj["advanceWidth"]?.Get<float>() ?? 0f;
            var glyphIndices = ReadGlyphIndices(entryObj["glyphIndices"]?.Get<SsbfArray>() ?? new SsbfArray());
            var sprite = new Sprite
            {
                AdvanceWidth = advanceWidth,
                GlyphIndices = glyphIndices
            };
            sprites[name] = sprite;
        }
        return sprites;
    }
    
    private static List<int> ReadGlyphIndices(SsbfArray arr)
    {
        var glyphIndices = new List<int>(arr.Count);
        foreach (var entry in arr)
        {
            var index = entry?.Get<int>() ?? 0;
            glyphIndices.Add(index);
        }
        return glyphIndices;
    }
    
    private static T[] ReadBuffer<T>(SsbfByteArray arr) where T : unmanaged
    {
        var bytes = arr.Data.AsSpan();
        var items = MemoryMarshal.Cast<byte, T>(bytes);
        return [..items];
    }
}