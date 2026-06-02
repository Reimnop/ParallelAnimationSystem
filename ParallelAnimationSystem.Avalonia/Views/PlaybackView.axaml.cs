using Avalonia.Controls;
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
}