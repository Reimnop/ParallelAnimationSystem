using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ParallelAnimationSystem.Avalonia.Controls;

/// <summary>
/// Converts (Value, Minimum, Maximum, TrackWidth) to pixel width for the fill bar.
/// </summary>
public sealed class NormalizedToWidthConverter : IMultiValueConverter
{
    public static NormalizedToWidthConverter Instance { get; } = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryUnpack(values, out var norm, out var trackWidth))
            return 0.0;

        return norm * trackWidth;
    }

    private static bool TryUnpack(IList<object?> values, out double norm, out double trackWidth)
    {
        norm = 0;
        trackWidth = 0;

        if (values.Count < 4
            || values[0] is not double value
            || values[1] is not double min
            || values[2] is not double max
            || values[3] is not double width)
            return false;

        var range = max - min;
        norm = range <= 0 ? 0 : Math.Clamp((value - min) / range, 0.0, 1.0);
        trackWidth = width;
        return true;
    }
}

/// <summary>
/// Converts (Value, Minimum, Maximum, TrackWidth) to left for the thumb
/// </summary>
public sealed class NormalizedToLeftConverter : IMultiValueConverter
{
    public static NormalizedToLeftConverter Instance { get; } = new();

    // Half the thumb's natural width; keep in sync with the template's Width="14".
    private const double HalfThumbWidth = 7.0;

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 4
            || values[0] is not double value
            || values[1] is not double min
            || values[2] is not double max
            || values[3] is not double trackWidth)
            return new Thickness(0);

        var range = max - min;
        var norm = range <= 0 ? 0 : Math.Clamp((value - min) / range, 0.0, 1.0);
        var left = norm * trackWidth - HalfThumbWidth;

        return left;
    }
}
