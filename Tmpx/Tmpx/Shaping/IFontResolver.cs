using System.Diagnostics.CodeAnalysis;
using Tmpx.Common;

namespace Tmpx.Shaping;

public interface IFontResolver
{
    bool TryResolve(string name, FontStyle style, [MaybeNullWhen(false)] out IFont font);
}