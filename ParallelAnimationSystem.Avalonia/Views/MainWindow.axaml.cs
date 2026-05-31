using Avalonia.Interactivity;
using ParallelAnimationSystem.Avalonia.Controls;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class MainWindow : ShadUI.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void TimelineScrubber_OnSeekRequested(object? sender, SeekRequestedEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        vm.Seek(e.Position);
    }
}