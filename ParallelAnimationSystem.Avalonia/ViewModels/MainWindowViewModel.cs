using System;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;
using ShadUI;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

[ViewModelView<MainWindow>]
public partial class MainWindowViewModel(DialogManager dialogManager, PlaybackViewModel playbackViewModel, SettingsViewModel settingsViewModel)
    : ViewModelBase, IDisposable
{
    public static FuncValueConverter<int, string> FpsTextConverter
        => new(x => $"{x} FPS");
    
    public static FuncValueConverter<bool, float> BoolToOpacityConverter
        => new(x => x ? 1f : 0f);
    
    public DialogManager DialogManager { get; } = dialogManager;
    public PlaybackViewModel PlaybackViewModel { get; } = playbackViewModel;
    public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }
    
    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    public void Dispose()
    {
        DialogManager.Dispose();
        PlaybackViewModel.Dispose();
    }
}
