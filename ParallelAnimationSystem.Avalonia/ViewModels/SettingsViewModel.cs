using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

[ViewModelView<SettingsView>]
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool EnablePostProcessing { get; set; } = true;

    [ObservableProperty]
    public partial bool EnableTextRendering { get; set; } = true;

    public event EventHandler? Closing;

    [RelayCommand]
    public void Close()
    {
        Closing?.Invoke(this, EventArgs.Empty);
    }
}
