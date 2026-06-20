using System;
using CommunityToolkit.Mvvm.Input;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

[ViewModelView<SettingsView>]
public partial class SettingsViewModel : ViewModelBase
{
    public event EventHandler? Closing;

    [RelayCommand]
    public void Close()
    {
        Closing?.Invoke(this, EventArgs.Empty);
    }
}
