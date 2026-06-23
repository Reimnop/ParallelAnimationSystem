using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace ParallelAnimationSystem.Avalonia.Controls;

/// <summary>
/// Arguments for <see cref="TimelineScrubber.SeekRequested"/>.
/// </summary>
public sealed class SeekRequestedEventArgs(double position) : EventArgs
{
    /// <summary>
    /// Normalized position in [0, 1].
    /// </summary>
    public double Position { get; } = position;
}

/// <summary>
/// A timeline scrubber for playback.
/// </summary>
[TemplatePart("PART_Track", typeof(Control))]
[TemplatePart("PART_Thumb", typeof(Control))]
public sealed class TimelineScrubber : TemplatedControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<TimelineScrubber, double>(nameof(Minimum), defaultValue: 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<TimelineScrubber, double>(nameof(Maximum), defaultValue: 1.0);

    /// <summary>
    /// The current playback position. This control never writes to this property.
    /// Update it from outside (e.g. on a playback tick) to move the thumb.
    /// </summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<TimelineScrubber, double>(nameof(Value), defaultValue: 0.0);

    /// <summary>
    /// Minimum pointer travel (px) before a press becomes a drag.
    /// Below this threshold a release fires a seek at the click point.
    /// </summary>
    public static readonly DirectProperty<TimelineScrubber, double> SeekDeadZoneProperty =
        AvaloniaProperty.RegisterDirect<TimelineScrubber, double>(
            nameof(SeekDeadZone),
            o => o.SeekDeadZone,
            (o, v) => o.SeekDeadZone = v);

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double SeekDeadZone
    {
        get;
        set => SetAndRaise(SeekDeadZoneProperty, ref field, value);
    } = 4.0;

    // Pseudo classes:
    // :dragging - pointer is captured and moving
    // :hovering - pointer is over the track or thumb

    /// <summary>
    /// Raised for every user interaction that implies a new playback position:
    /// track click, thumb drag (continuously), and keyboard nudges.
    /// The control never acts on this itself.
    /// </summary>
    public event EventHandler<SeekRequestedEventArgs>? SeekRequested;

    private Control? trackPart;
    private Control? thumbPart;

    private bool isDragging;
    private Point dragStartPoint;
    private bool dragThresholdExceeded;

    static TimelineScrubber()
    {
        ValueProperty.Changed.AddClassHandler<TimelineScrubber>(
            (o, _) => o.UpdatePseudoClasses());

        FocusableProperty.OverrideDefaultValue<TimelineScrubber>(true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Detach from old parts
        if (trackPart is not null)
        {
            trackPart.PointerPressed -= OnTrackPointerPressed;
            trackPart.PointerMoved -= OnTrackPointerMoved;
            trackPart.PointerReleased -= OnTrackPointerReleased;
            trackPart.PointerCaptureLost -= OnTrackPointerCaptureLost;
            trackPart.PointerEntered -= OnTrackPointerEntered;
            trackPart.PointerExited -= OnTrackPointerExited;
        }
        
        trackPart = e.NameScope.Find<Control>("PART_Track");
        thumbPart = e.NameScope.Find<Control>("PART_Thumb");

        if (trackPart is not null)
        {
            trackPart.PointerPressed += OnTrackPointerPressed;
            trackPart.PointerMoved += OnTrackPointerMoved;
            trackPart.PointerReleased += OnTrackPointerReleased;
            trackPart.PointerCaptureLost += OnTrackPointerCaptureLost;
            trackPart.PointerEntered += OnTrackPointerEntered;
            trackPart.PointerExited += OnTrackPointerExited;
        }

        UpdatePseudoClasses();
    }

    private void OnTrackPointerEntered(object? sender, PointerEventArgs e)
    {
        PseudoClasses.Set(":hovering", true);
    }

    private void OnTrackPointerExited(object? sender, PointerEventArgs e)
    {
        if (!isDragging)
            PseudoClasses.Set(":hovering", false);
    }

    private void OnTrackPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (trackPart is null || !e.GetCurrentPoint(trackPart).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(trackPart);
        isDragging = true;
        dragThresholdExceeded = false;
        dragStartPoint = e.GetPosition(trackPart);

        PseudoClasses.Set(":dragging", true);

        // Fire immediately so clicking anywhere on the track seeks on press
        RaiseSeek(NormalizeX(dragStartPoint.X, trackPart.Bounds.Width));
        e.Handled = true;
    }

    void OnTrackPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDragging || trackPart is null)
            return;

        Point pos = e.GetPosition(trackPart);

        if (!dragThresholdExceeded)
        {
            double travel = Math.Abs(pos.X - dragStartPoint.X);
            if (travel >= SeekDeadZone)
                dragThresholdExceeded = true;
        }

        if (dragThresholdExceeded)
            RaiseSeek(NormalizeX(pos.X, trackPart.Bounds.Width));

        e.Handled = true;
    }

    private void OnTrackPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isDragging || trackPart is null)
            return;

        // If the pointer never exceeded the dead zone we already fired on press,
        // so nothing extra is needed, just clean up.
        isDragging = false;
        dragThresholdExceeded = false;
        e.Pointer.Capture(null);

        PseudoClasses.Set(":dragging", false);
        PseudoClasses.Set(":hovering", trackPart.IsPointerOver);
        e.Handled = true;
    }

    private void OnTrackPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!isDragging)
            return;

        isDragging = false;
        dragThresholdExceeded = false;
        PseudoClasses.Set(":dragging", false);
        PseudoClasses.Set(":hovering", false);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 0.05 : 0.01;

        var current = NormalizedValue();
        var next = e.Key switch
        {
            Key.Left or Key.Down => current - step,
            Key.Right or Key.Up => current + step,
            Key.Home => 0.0,
            Key.End => 1.0,
            _ => double.NaN
        };

        if (double.IsNaN(next))
            return;

        RaiseSeek(Math.Clamp(next, 0.0, 1.0));
        e.Handled = true;
    }

    private void RaiseSeek(double normalizedPosition)
    {
        var absolute = double.Lerp(Minimum, Maximum, normalizedPosition);
        SeekRequested?.Invoke(this, new SeekRequestedEventArgs(absolute));
    }

    private static double NormalizeX(double x, double width) =>
        width > 0 ? Math.Clamp(x / width, 0.0, 1.0) : 0.0;

    private double NormalizedValue()
    {
        var range = Maximum - Minimum;
        return range <= 0 ? 0.0 : Math.Clamp((Value - Minimum) / range, 0.0, 1.0);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":at-start", NormalizedValue() <= 0.0);
        PseudoClasses.Set(":at-end", NormalizedValue() >= 1.0);
    }
}
