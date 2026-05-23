using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Avalonia.Integration;
using ParallelAnimationSystem.Core.Service;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AudioPlayer audioPlayer;
    
    [ObservableProperty]
    public partial float Time { get; set; }

    public MainWindowViewModel()
    {
        audioPlayer = AudioPlayer.Load("H:\\PA Levels\\pam4.ogg");
    }

    [RelayCommand]
    public void OnTick()
    {
        Time = (float) audioPlayer.Position;
    }

    public void InitializePAS(PASControl control)
    {
        var sp = control.InternalServiceProvider;
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap("H:\\PA Levels\\pam4.vgd");
        
        audioPlayer.Play();
    }
}