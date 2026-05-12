using Tmpx.Common;

namespace Tmpx.Shaping;

public static class Extension
{
    extension(IFontResolver resolver)
    {
        public IFont Resolve(string family, FontStyle style)
        {
            if (resolver.TryResolve(family, style, out var font))
                return font;
        
            throw new InvalidOperationException($"Font name '{family}' with style '{style}' could not be resolved.");
        }
    }
}