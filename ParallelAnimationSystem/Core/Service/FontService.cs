using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering;
using SimpleStructuredBinaryFormat;
using Tmpx.Common;
using Tmpx.Shaping;

namespace ParallelAnimationSystem.Core.Service;

public class FontService : IDisposable, IFontResolver
{
    // Map from Tmpx IFont → FontInfo (holds GlobalShapeEntryBase for index remapping)
    private readonly Dictionary<IFont, FontInfo> infoByFont = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<(string Name, FontStyle Style), IFont> fontByNameAndStyle = new();
    private readonly FontFallbackChainRegistry fallbackChainRegistry = new("NotoSans");

    private readonly ResourceLoader resourceLoader;
    private readonly RenderQueue renderQueue;

    public Shaper Shaper { get; }

    public FontService(ResourceLoader resourceLoader, RenderQueue renderQueue)
    {
        this.resourceLoader = resourceLoader;
        this.renderQueue = renderQueue;

        // Load all fonts
        var inconsolata = LoadFont("Fonts/Inconsolata-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/Inconsolata-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/Inconsolata-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/Inconsolata-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var liberationSans = LoadFont("Fonts/LiberationSans-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/LiberationSans-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/LiberationSans-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/LiberationSans-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var majorMonoDisplay = LoadFont("Fonts/MajorMonoDisplay-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/MajorMonoDisplay-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/MajorMonoDisplay-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/MajorMonoDisplay-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var poorStory = LoadFont("Fonts/PoorStory-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/PoorStory-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/PoorStory-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/PoorStory-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var hellovetica = LoadFont("Fonts/Hellovetica-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/Hellovetica-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/Hellovetica-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/Hellovetica-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var notoMono = LoadFont("Fonts/NotoMono-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/NotoMono-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/NotoMono-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/NotoMono-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var notoSans = LoadFont("Fonts/NotoSans-Regular.tmpx", FontStyle.Regular);
        LoadFont("Fonts/NotoSans-Bold.tmpx", FontStyle.Bold);
        LoadFont("Fonts/NotoSans-Italic.tmpx", FontStyle.Italic);
        LoadFont("Fonts/NotoSans-BoldItalic.tmpx", FontStyle.Bold | FontStyle.Italic);
        
        var arialuni = LoadFont("Fonts/Arialuni-Regular.tmpx", FontStyle.Regular);
        var seguisym = LoadFont("Fonts/Seguisym-Regular.tmpx", FontStyle.Regular);
        var code2000 = LoadFont("Fonts/Code2000-Regular.tmpx", FontStyle.Regular);

        // Register fallback stacks
        RegisterFontChain("NotoSans", [notoSans, arialuni, seguisym, code2000]);
        RegisterFontChain("NotoMono", [notoMono, arialuni, seguisym, code2000]);
        RegisterFontChain("LiberationSans", [liberationSans, arialuni, seguisym, code2000]);
        RegisterFontChain("MajorMonoDisplay", [majorMonoDisplay, arialuni, seguisym, code2000]);
        RegisterFontChain("PoorStory", [poorStory, arialuni, seguisym, code2000]);
        RegisterFontChain("Hellovetica", [hellovetica, arialuni, seguisym, code2000]);
        RegisterFontChain("Inconsolata", [inconsolata, arialuni, seguisym, code2000]);

        // Concatenate buffers, assign global bases, upload
        BuildAndUploadBuffers();
        
        Shaper = new Shaper(this, fallbackChainRegistry);
    }
    
    public int GetGlobalShapeEntryBase(IFont font)
    {
        if (!infoByFont.TryGetValue(font, out var info))
            throw new ArgumentException("Unknown font, not registered with FontService", nameof(font));
        return info.GlobalShapeEntryBase;
    }

    bool IFontResolver.TryResolve(string name, FontStyle style, [MaybeNullWhen(false)] out IFont font)
    {
        font = null;

        name = name.ToLowerInvariant().Trim();
        if (name.EndsWith("sdf"))
            name = name[..^3].Trim();

        if (fontByNameAndStyle.TryGetValue((name, style), out var font_))
        {
            font = font_;
            return true;
        }
        
        return false;
    }

    private FontInfo LoadFont(string path, FontStyle style)
    {
        using var stream = resourceLoader.OpenResource(path)
            ?? throw new InvalidOperationException($"Could not open font asset '{path}'");

        if (SsbfRead.ReadFromStream(stream) is not SsbfObject root)
            throw new InvalidOperationException($"Font asset '{path}' is not a valid SSBF object");

        var font = TmpxReader.Read(root);
        
        // GlobalShapeEntryBase is 0 here; BuildAndUploadBuffers will overwrite it.
        var info = new FontInfo(font.FamilyName, style, font, globalShapeEntryBase: 0);
        infoByFont[font] = info;
        fontByNameAndStyle[(font.FamilyName.ToLowerInvariant().Trim(), style)] = font;
        return info;
    }

    private void RegisterFontChain(string name, IReadOnlyList<FontInfo> fonts)
    {
        fallbackChainRegistry.RegisterFallbackChain(name, fonts.Select(x => x.Name).ToArray());
    }

    private void BuildAndUploadBuffers()
    {
        // Walk every distinct loaded font (in stable insertion order via infoByFont) and
        // concatenate the four per-font buffers into four global arrays, recording each font's
        // base offset so indices can be remapped later.

        var allFonts = infoByFont.Values.ToList(); // insertion order

        // Compute running totals to determine base offsets
        var curveBase = 0;
        var curveIndexBase = 0;
        var bandEntryBase = 0;
        var shapeEntryBase = 0;

        foreach (var info in allFonts)
        {
            // Patch the GlobalShapeEntryBase in place.
            // FontInfo is a reference type so we re-create it with the correct base.
            var patched = new FontInfo(info.Name, info.Style, info.Font, shapeEntryBase);
            infoByFont[info.Font] = patched;

            curveBase += info.Font.Curves.Length;
            curveIndexBase += info.Font.CurveIndices.Length;
            bandEntryBase += info.Font.BandEntries.Length;
            shapeEntryBase += info.Font.ShapeEntries.Length;
        }

        // Allocate the four global buffers
        var globalCurves       = new QuadraticCurve[curveBase];
        var globalCurveIndices = new int[curveIndexBase];
        var globalBandEntries  = new BandEntry[bandEntryBase];
        var globalShapeEntries = new ShapeEntry[shapeEntryBase];

        // Second pass: fill the global buffers, adjusting intra-font indices
        var curveOff = 0;
        var curveIndexOff = 0;
        var bandEntryOff = 0;
        var shapeEntryOff = 0;

        // Iterate in the same order (re-read the patched infos)
        foreach (var originalInfo in allFonts)
        {
            var info = infoByFont[originalInfo.Font]; // pick up the patched version
            var font = info.Font;

            // Curves
            font.Curves.CopyTo(globalCurves, curveOff);

            // CurveIndices: each index is a local curve index, add curveOff
            for (var i = 0; i < font.CurveIndices.Length; i++)
                globalCurveIndices[curveIndexOff + i] = font.CurveIndices[i] + curveOff;

            // BandEntries: each entry's CurveIndexBaseIndex is local, add curveIndexOff
            for (var i = 0; i < font.BandEntries.Length; i++)
            {
                var be = font.BandEntries[i];
                globalBandEntries[bandEntryOff + i] = new BandEntry
                {
                    CurveIndexBaseIndex = be.CurveIndexBaseIndex + curveIndexOff,
                    CurveIndexCount     = be.CurveIndexCount,
                };
            }

            // ShapeEntries: horizontal/vertical band base indices are local, add bandEntryOff
            for (var i = 0; i < font.ShapeEntries.Length; i++)
            {
                var se = font.ShapeEntries[i];
                globalShapeEntries[shapeEntryOff + i] = new ShapeEntry
                {
                    HorizontalBandEntryBaseIndex = se.HorizontalBandEntryBaseIndex + bandEntryOff,
                    HorizontalBandEntryCount     = se.HorizontalBandEntryCount,
                    HorizontalBandScale          = se.HorizontalBandScale,
                    HorizontalBandOffset         = se.HorizontalBandOffset,

                    VerticalBandEntryBaseIndex   = se.VerticalBandEntryBaseIndex + bandEntryOff,
                    VerticalBandEntryCount       = se.VerticalBandEntryCount,
                    VerticalBandScale            = se.VerticalBandScale,
                    VerticalBandOffset           = se.VerticalBandOffset,

                    Min   = se.Min,
                    Max   = se.Max,
                    Color = se.Color,
                };
            }

            curveOff      += font.Curves.Length;
            curveIndexOff += font.CurveIndices.Length;
            bandEntryOff  += font.BandEntries.Length;
            shapeEntryOff += font.ShapeEntries.Length;
        }

        // Upload to renderer in one shot
        renderQueue.SetFontBuffers(globalCurves, globalCurveIndices, globalBandEntries, globalShapeEntries);
    }

    public void Dispose()
    {
        renderQueue.SetFontBuffers([], [], [], []);
    }
}
