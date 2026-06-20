using System;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

[ViewModelView<MainWindow>]
public partial class MainWindowViewModel
    : ViewModelBase, IDisposable
{
    public static FuncValueConverter<int, string> FpsTextConverter
        => new(x => $"{x} FPS");
    
    public PlaybackViewModel PlaybackViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }
    
    public MainWindowViewModel(PlaybackViewModel playbackViewModel, SettingsViewModel settingsViewModel)
    {
        PlaybackViewModel = playbackViewModel;
        SettingsViewModel = settingsViewModel;
        
        SettingsViewModel.Closing += (_, _) => IsSettingsOpen = false;
    }
    
    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    public void Dispose()
    {
        PlaybackViewModel.Dispose();
    }
}
