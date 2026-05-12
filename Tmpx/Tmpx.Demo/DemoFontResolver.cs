using Tmpx.Common;
using Tmpx.Shaping;

namespace Tmpx.Demo;

public class DemoFontResolver(Font font) : IFontResolver
{
    public bool TryResolve(string name, FontStyle style, out IFont font1)
    {
        font1 = font;
        return true;
    }
}