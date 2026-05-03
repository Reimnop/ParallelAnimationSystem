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
        Description = "Character range(s) to include in the output font. " +
                      "Each value can be a single codepoint ('U+0021'), an inclusive range ('U+0020-U+007E'), " +
                      "or a comma-separated list of either ('U+0021,U+00A4-U+00FF,U+25A0-U+25E0'). " +
                      "Can be specified multiple times.",
        Name = "character-range",
        Alias = "c",
        Required = true)]
    public List<string> CharacterRanges { get; set; } = [];
    
    [CliOption(
        Description = "Whether to apply a fake italic transformation to the glyphs.",
        Name = "fake-italic",
        Alias = "fi",
        Required = false)]
    public bool FakeItalic { get; set; }
    
    [CliOption(
        Description = "Whether to embolden the glyphs.",
        Name = "fake-bold",
        Alias = "fb",
        Required = false)]
    public bool FakeBold { get; set; }
    
    [CliOption(
        Description = "Family name of the generated font, set to override the name in the input font file.",
        Name = "family-name",
        Alias = "fn",
        Required = false)]
    public string? FamilyName { get; set; }
    
    [CliOption(
        Description = "Style name of the generated font, set to override the style name in the input font file.",
        Name = "style-name",
        Alias = "sn",
        Required = false)]
    public string? StyleName { get; set; }
    
    [CliOption(
        Description = "Amount of horizontal and vertical bands to split each glyph into. " +
                      "Higher values result in better runtime performance but uses more memory.",
        Name = "bands",
        Alias = "b",
        Required = false)]
    public int Bands { get; set; } = 16;

    public void Run()
    {
        // parse character ranges (each entry may itself be a comma-separated list)
        var characterRanges = CharacterRanges.SelectMany(CharacterRange.ParseList).ToList();
        
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
            
            var outline = glyph.Outline;
            
            if (FakeItalic)
            {
                outline.Transform(new FTMatrix
                {
                    XX = Fixed16Dot16.FromSingle(1f),
                    XY = Fixed16Dot16.FromSingle(0.21f),
                    YX = Fixed16Dot16.FromSingle(0f),
                    YY = Fixed16Dot16.FromSingle(1f)
                });
            }
            
            if (FakeBold)
            {
                outline.Embolden(Fixed26Dot6.FromRawValue((int)(unitsPerEm * 0.03f)));
            }
            
            var glyphMetrics = glyph.Metrics;

            var bbox = outline.GetBBox();
            
            var min = new Vector2(
                bbox.Left / unitsPerEm,
                bbox.Bottom / unitsPerEm);
            var max = new Vector2(
                bbox.Right / unitsPerEm,
                bbox.Top / unitsPerEm);
            
            var advanceWidth = glyphMetrics.HorizontalAdvance.Value / unitsPerEm;
            
            if (outline.ContoursCount == 0)
            {
                glyphMap[codepoint] = new Glyph
                {
                    AdvanceWidth = advanceWidth,
                    ShapeEntryIndex = -1
                };
                continue;
            }
            
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
            FamilyName = FamilyName ?? face.FamilyName,
            StyleName = StyleName ?? face.StyleName,
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