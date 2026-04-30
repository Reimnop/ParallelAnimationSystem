using System.Numerics;
using DotMake.CommandLine;
using SharpFont;
using SimpleStructuredBinaryFormat;
using Tmpx.Common;
using Glyph = Tmpx.Common.Glyph;

namespace Tmpx.Generator;

[CliCommand(
    Description = "Generates a TMPX font file from a FreeType-compatible font file.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class GenerateCommand
{
    [CliOption(
        Description = "The path to the input font file. (e.g. .ttf or .otf)",
        Name = "input",
        Alias = "i",
        Required = true)]
    public required string InputPath { get; set; }
    
    [CliOption(
        Description = "The path to the output TMPX font file.",
        Name = "output",
        Alias = "o",
        Required = true)]
    public required string OutputPath { get; set; }
    
    [CliOption(
        Description = "Character range to include in the output font, in the format 'U+XXXX-U+YYYY' (inclusive). " +
                      "Can be specified multiple times for multiple ranges.",
        Name = "character-range",
        Alias = "c",
        Required = true)]
    public List<string> CharacterRanges { get; set; } = [];
    
    [CliOption(
        Description = "Amount of horizontal and vertical bands to split each glyph into. " +
                      "Higher values result in better runtime performance but uses more memory.",
        Name = "bands",
        Alias = "b",
        Required = false)]
    public int Bands { get; set; } = 16;

    public void Run()
    {
        // parse character ranges
        var characterRanges = CharacterRanges.Select(CharacterRange.FromString).ToList();
        
        // load freetype
        var library = new Library();
        var face = new Face(library, InputPath);

        var unitsPerEm = (float)face.UnitsPerEM;
        
        // extract glyphs
        var glyphPacker = new GlyphPacker();
        var glyphMap = new Dictionary<char, Glyph>();

        foreach (var codepoint in characterRanges.SelectMany(x => x.Enumerate()))
        {
            var glyphIndex = face.GetCharIndex(codepoint);
            if (glyphIndex == 0)
            {
                Console.WriteLine($"Warning: codepoint U+{(int)codepoint:X4} ('{codepoint}') not found in font");
                continue;
            }
            
            face.LoadGlyph(glyphIndex, LoadFlags.NoScale | LoadFlags.NoHinting, LoadTarget.Normal);
            
            var glyph = face.Glyph;
            var glyphMetrics = glyph.Metrics;
            
            var min = new Vector2(
                glyphMetrics.HorizontalBearingX.Value / unitsPerEm,
                (glyphMetrics.HorizontalBearingY - glyphMetrics.Height).Value / unitsPerEm);
            var max = new Vector2(
                (glyphMetrics.HorizontalBearingX + glyphMetrics.Width).Value / unitsPerEm,
                glyphMetrics.HorizontalBearingY.Value / unitsPerEm);
            var advanceWidth = glyphMetrics.HorizontalAdvance.Value / unitsPerEm;
            
            if (glyph.Outline.ContoursCount == 0)
            {
                glyphMap[codepoint] = new Glyph
                {
                    AdvanceWidth = advanceWidth,
                    ShapeEntryIndex = -1
                };
                continue;
            }
            
            var outline = glyph.Outline;
            var glyphCurves = CurveExtractor.FromOutline(outline, unitsPerEm);
            BandAccelerator.Process(
                glyphCurves, 
                min, max, 
                Bands,
                out var glyphCurveIndices,
                out var glyphHorizontalBandEntries,
                out var glyphVerticalBandEntries);
            
            glyphMap[codepoint] = glyphPacker.AddGlyph(
                glyphCurves, 
                glyphCurveIndices, 
                glyphHorizontalBandEntries, 
                glyphVerticalBandEntries, 
                advanceWidth,
                min, max,
                Color.White);
        }
        
        // write to file
        var font = new Font
        {
            FamilyName = face.FamilyName,
            StyleName = face.StyleName,
            Metrics = new FontMetrics
            {
                Ascender = face.Ascender / unitsPerEm,
                Descender = face.Descender / unitsPerEm,
                LineHeight = face.Height / unitsPerEm,
            },
            GlyphMap = glyphMap,
            Curves = glyphPacker.Curves.ToArray(),
            CurveIndices = glyphPacker.CurveIndices.ToArray(),
            BandEntries = glyphPacker.BandEntries.ToArray(),
            ShapeEntries = glyphPacker.ShapeEntries.ToArray()
        };
        
        var obj = TmpxWriter.Write(font);

        using var stream = File.Create(OutputPath);
        SsbfWrite.WriteToStream(stream, obj, true);
    }
}