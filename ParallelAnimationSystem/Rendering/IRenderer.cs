using System.Numerics;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Windowing;
using TmpParser;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderer : IDisposable
{
    IWindow Window { get; }
    int QueuedDrawListCount { get; }
    
    void Initialize();
    
    IMeshHandle RegisterMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    IFontHandle RegisterFont(Stream stream);
    ITextHandle CreateText(
        string str, 
        IReadOnlyList<FontStack> fontStacks, 
        string defaultFontName,
        HorizontalAlignment horizontalAlignment, 
        VerticalAlignment verticalAlignment);
    
    IDrawList GetDrawList();
    void SubmitDrawList(IDrawList drawList);

    bool ProcessFrame();
}