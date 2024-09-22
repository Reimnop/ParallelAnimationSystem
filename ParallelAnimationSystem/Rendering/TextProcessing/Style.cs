using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public readonly record struct Style(
    bool Bold, 
    bool Italic, 
    bool Underline,
    ColorAlpha Color);