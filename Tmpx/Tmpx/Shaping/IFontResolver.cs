using System.Diagnostics.CodeAnalysis;
using Tmpx.Common;

namespace Tmpx.Shaping;

public interface IFontResolver
{
    bool TryResolve(string family, FontStyle style, [MaybeNullWhen(false)] out IFont font);
}