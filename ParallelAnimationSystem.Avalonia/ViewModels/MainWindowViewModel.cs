using System;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;
using ShadUI;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

[ViewModelView<MainWindow>]
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    public static FuncValueConverter<int, string> FpsTextConverter
        => new(x => $"{x} FPS");
    
    [ObservableProperty]
    public partial DialogManager DialogManager { get; set; }
    
    [ObservableProperty]
    public partial PlaybackViewModel PlaybackViewModel { get; set; }

    public MainWindowViewModel(DialogManager dialogManager, PlaybackViewModel playbackViewModel)
    {
        DialogManager = dialogManager;
        PlaybackViewModel = playbackViewModel;
    }

    public void Dispose()
    {
        DialogManager.Dispose();
        PlaybackViewModel.Dispose();
    }
}
