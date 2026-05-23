using Avalonia.Interactivity;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class MainWindow : ShadUI.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        vm.InitializePAS(PasControl);
    }
}