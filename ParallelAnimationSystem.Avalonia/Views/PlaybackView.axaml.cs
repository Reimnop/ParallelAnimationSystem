using Avalonia.Controls;
using Avalonia.Input;
using ParallelAnimationSystem.Avalonia.Controls;
using ParallelAnimationSystem.Avalonia.ViewModels;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class PlaybackView : UserControl
{
    public PlaybackView()
    {
        InitializeComponent();
    }
    
    private void TimelineScrubber_OnSeekRequested(object? sender, SeekRequestedEventArgs e)
    {
        if (DataContext is not PlaybackViewModel vm)
            return;

        vm.Seek(e.Position);
    }

    private void PlaybackRootPanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not PlaybackViewModel vm)
            return;

        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            e.Handled = true;
            vm.PlayPause();
        }
    }

    private void PlaybackControlsBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            e.Handled = true;
    }

    private void PlaybackControlsBorder_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
            e.Handled = true;
    }
}