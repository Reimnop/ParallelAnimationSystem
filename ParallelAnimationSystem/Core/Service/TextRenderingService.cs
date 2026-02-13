using System.Numerics;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Text;
using TmpParser;

namespace ParallelAnimationSystem.Core.Service;

public class TextRenderingService
{
    private readonly TextShaper textShaper;
    
    private readonly ResourceLoader resourceLoader;
    private readonly IRenderingFactory renderingFactory;
    
    public TextRenderingService(ResourceLoader resourceLoader, IRenderingFactory renderingFactory)
    {
        this.resourceLoader = resourceLoader;
        this.renderingFactory = renderingFactory;
        
        var fonts = LoadFonts();
        textShaper = new TextShaper(fonts);
    }
    
    public ShapedRichText ShapeText(string text, Vector2 origin)
        => textShaper.ShapeText(
            text,
            "NotoMono SDF",
            origin.X switch
            {
                -0.5f => HorizontalAlignment.Right,
                0.5f => HorizontalAlignment.Left,
                _ => HorizontalAlignment.Center,
            },
            origin.Y switch
            {
                -0.5f => VerticalAlignment.Top,
                0.5f => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center,
            });
    
    private FontCollection LoadFonts()
    {
        var inconsolata = LoadFont("Fonts/Inconsolata.tmpe");
        var arialuni = LoadFont("Fonts/Arialuni.tmpe");
        var seguisym = LoadFont("Fonts/Seguisym.tmpe");
        var code2000 = LoadFont("Fonts/Code2000.tmpe");
        var inconsolataStack = new FontStack("Inconsolata SDF", 16.0f, [inconsolata, arialuni, seguisym, code2000]);
        
        var liberationSans = LoadFont("Fonts/LiberationSans.tmpe");
        var liberationSansStack = new FontStack("LiberationSans SDF", 16.0f, [liberationSans, arialuni, seguisym, code2000]);
        
        var notoMono = LoadFont("Fonts/NotoMono.tmpe");
        var notoMonoStack = new FontStack("NotoMono SDF", 16.0f, [notoMono, arialuni, seguisym, code2000]);
            
        return new FontCollection([inconsolataStack, liberationSansStack, notoMonoStack]);
    }
    
    private IFont LoadFont(string path)
    {
        using var stream = resourceLoader.OpenResource(path);
        if (stream is null)
            throw new InvalidOperationException($"Could not load font data at '{path}'");
        return renderingFactory.CreateFont(stream);
    }
}