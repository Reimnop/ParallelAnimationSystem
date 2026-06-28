using System;
using System.ComponentModel;
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
        SettingsViewModel.PropertyChanged += SettingsViewModelOnPropertyChanged;
    }

    private void SettingsViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var director = PlaybackViewModel.PASViewModel.Director;

        switch (e.PropertyName)
        {
            case nameof(SettingsViewModel.EnablePostProcessing):
                director.EnablePostProcessing = SettingsViewModel.EnablePostProcessing;
                break;
            case nameof(SettingsViewModel.EnableTextRendering):
                director.EnableTextRendering = SettingsViewModel.EnableTextRendering;
                break;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }
    
    public void OnPlayPauseKey()
    {
        PlaybackViewModel.PlayPauseDisplayIcon();
    }

    public void Dispose()
    {
        PlaybackViewModel.Dispose();
    }
}
