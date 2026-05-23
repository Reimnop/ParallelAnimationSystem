using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Service;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class MainWindow : Window
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