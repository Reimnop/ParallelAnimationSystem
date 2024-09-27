using OpenTK.Mathematics;
using ParallelAnimationSystem.Rendering.TextProcessing;
using TmpParser;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderer : IDisposable
{
    bool ShouldExit { get; }
    int QueuedDrawListCount { get; }
    
    void Initialize();
    
    IMeshHandle RegisterMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    IFontHandle RegisterFont(Stream stream);
    ITextHandle CreateText(
        string str, 
        IEnumerable<FontStack> fontStacks, 
        string defaultFontName,
        HorizontalAlignment horizontalAlignment, 
        VerticalAlignment verticalAlignment);
    
    IDrawList GetDrawList();
    void SubmitDrawList(IDrawList drawList);

    void ProcessFrame();
}