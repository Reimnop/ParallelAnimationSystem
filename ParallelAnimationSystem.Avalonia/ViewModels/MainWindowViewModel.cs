using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Avalonia.Integration;
using ParallelAnimationSystem.Core.Service;
using Window = ShadUI.Window;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private AudioPlayer audioPlayer;
    private PASControl pasControl = null!;

    private bool updatingFromTick;
    
    [ObservableProperty]
    public partial float Time { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial double SeekPosition { get; set; }

    public double Duration { get; private set; }

    public string CurrentTimeText => TimeSpan.FromSeconds(SeekPosition).ToString(@"m\:ss");
    public string DurationText => TimeSpan.FromSeconds(Duration).ToString(@"m\:ss");
    public LucideIconKind PlayPauseIcon => IsPlaying ? LucideIconKind.Pause : LucideIconKind.Play;

    public MainWindowViewModel()
    {
        audioPlayer = AudioPlayer.Load("H:\\PA Levels\\pam4.ogg");
    }

    partial void OnIsPlayingChanged(bool value) 
        => OnPropertyChanged(nameof(PlayPauseIcon));

    partial void OnSeekPositionChanged(double value)
    {
        if (!updatingFromTick)
        {
            audioPlayer.Position = value;
            Time = (float)value;
            OnPropertyChanged(nameof(CurrentTimeText));
        }
    }

    [RelayCommand]
    public async Task OnOpenBeatmapAsync(Window window)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel == null)
            return;
        
        var beatmapOptions = new FilePickerOpenOptions
        {
            Title = "Open Beatmap",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Beatmap File")
                {
                    Patterns = ["*.vgd", "*.lsb"]
                }
            ]
        };
        
        var beatmapFiles = await topLevel.StorageProvider.OpenFilePickerAsync(beatmapOptions);
        if (beatmapFiles.Count == 0)
            return;
        
        var beatmapFile = beatmapFiles[0];
        var beatmapPath = beatmapFile.TryGetLocalPath();
        if (beatmapPath == null)
            return;
        
        var audioOptions = new FilePickerOpenOptions
        {
            Title = "Open Audio",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio File")
                {
                    Patterns = ["*.ogg"]
                }
            ]
        };
        
        var audioFiles = await topLevel.StorageProvider.OpenFilePickerAsync(audioOptions);
        if (audioFiles.Count == 0)            
            return;
        
        var audioFile = audioFiles[0];
        var audioPath = audioFile.TryGetLocalPath();
        if (audioPath == null)            
            return;
        
        var sp = pasControl.InternalServiceProvider;
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapPath);
        
        audioPlayer.Dispose();
        audioPlayer = AudioPlayer.Load(audioPath);
        Duration = audioPlayer.Length;
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(DurationText));
    }

    [RelayCommand]
    public void OnTick()
    {
        updatingFromTick = true;
        SeekPosition = audioPlayer.Position;
        Time = (float)audioPlayer.Position;
        IsPlaying = audioPlayer.Playing;
        OnPropertyChanged(nameof(CurrentTimeText));
        updatingFromTick = false;
    }

    [RelayCommand]
    public void PlayPause()
    {
        if (audioPlayer.Playing)
            audioPlayer.Pause();
        else
            audioPlayer.Play();
        IsPlaying = audioPlayer.Playing;
    }

    public void InitializePAS(PASControl control)
    {
        pasControl = control;
        
        var sp = pasControl.InternalServiceProvider;
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap("H:\\PA Levels\\pam4.vgd");

        Duration = audioPlayer.Length;
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(DurationText));

        audioPlayer.Play();
        IsPlaying = true;
    }
}
