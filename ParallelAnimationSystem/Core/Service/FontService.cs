using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.Handle;
using ParallelAnimationSystem.Util;
using TmpIO;

namespace ParallelAnimationSystem.Core.Service;

public class FontService : IDisposable
{
    private readonly ResourceLoader resourceLoader;
    private readonly IRenderQueue renderQueue;
    
    private readonly Dictionary<string, FontStack> fontStacks = new();
    private readonly List<FontInfo?> fontInfos = [];
    
    public FontService(ResourceLoader resourceLoader, IRenderQueue renderQueue)
    {
        this.resourceLoader = resourceLoader;
        this.renderQueue = renderQueue;
        
        var inconsolata = LoadFont("Fonts/Inconsolata.tmpe");
        var arialuni = LoadFont("Fonts/Arialuni.tmpe");
        var seguisym = LoadFont("Fonts/Seguisym.tmpe");
        var code2000 = LoadFont("Fonts/Code2000.tmpe");
        var inconsolataStack = new FontStack("Inconsolata SDF", 16.0f, [inconsolata, arialuni, seguisym, code2000]);
        fontStacks.Add(inconsolataStack.Name.ToLowerInvariant(), inconsolataStack);
        
        var liberationSans = LoadFont("Fonts/LiberationSans.tmpe");
        var liberationSansStack = new FontStack("LiberationSans SDF", 16.0f, [liberationSans, arialuni, seguisym, code2000]);
        fontStacks.Add(liberationSansStack.Name.ToLowerInvariant(), liberationSansStack);
        
        var notoMono = LoadFont("Fonts/NotoMono.tmpe");
        var notoMonoStack = new FontStack("NotoMono SDF", 16.0f, [notoMono, arialuni, seguisym, code2000]);
        fontStacks.Add(notoMonoStack.Name.ToLowerInvariant(), notoMonoStack);
    }
    
    public void Dispose()
    {
        for (var i = 0; i < fontInfos.Count; i++)
        {
            var fontInfo = fontInfos[i];
            if (fontInfo is not null)
                renderQueue.DestroyFont(new FontHandle(i));
        }
    }
    
    public bool TryGetFontStack(string name, [MaybeNullWhen(false)] out FontStack fontStack)
        => fontStacks.TryGetValue(name.ToLowerInvariant(), out fontStack);
    
    public bool TryGetFontInfo(FontHandle fontHandle, [MaybeNullWhen(false)] out FontInfo fontInfo)
    {
        if (fontHandle.Id < 0 || fontHandle.Id >= fontInfos.Count)
        {
            fontInfo = null;
            return false;
        }
        fontInfo = fontInfos[fontHandle.Id];
        return fontInfo is not null;
    }
    
    private FontHandle LoadFont(string path)
    {
        using var stream = resourceLoader.OpenResource(path);
        if (stream is null)
            throw new InvalidOperationException($"Could not load font data at '{path}'");

        var tmpFile = TmpRead.Read(stream);
        var atlas = tmpFile.Atlas;
        
        var fontHandle = renderQueue.CreateFont(atlas.Width, atlas.Height, MemoryMarshal.AsBytes(atlas.Data));
        var id = fontHandle.Id;
        
        fontInfos.EnsureCount(id + 1);
        fontInfos[id] = new FontInfo(
            tmpFile.Metadata,
            tmpFile.Characters.ToDictionary(x => x.Character),
            tmpFile.Glyphs.ToDictionary(x => x.Id));
        
        return fontHandle;
    }
}