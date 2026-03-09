using TmpParser;

namespace ParallelAnimationSystem.Core.Text;

public readonly record struct Style(
    bool Bold, 
    bool Italic, 
    bool Underline,
    ColorAlpha Color);