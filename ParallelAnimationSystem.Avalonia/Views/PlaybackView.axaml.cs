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
}