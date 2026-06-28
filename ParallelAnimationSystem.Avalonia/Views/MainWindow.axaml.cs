using Avalonia.Input;
using ParallelAnimationSystem.Avalonia.ViewModels;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class MainWindow : ShadUI.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (e.Key == Key.Space)
        {
            e.Handled = true;
            vm.OnPlayPauseKey();
        }
    }
}