using Avalonia.Controls;
using Avalonia.Input;
using ParallelAnimationSystem.Avalonia.ViewModels;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        if (DataContext is not SettingsViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            vm.Close();
        }
    }
}