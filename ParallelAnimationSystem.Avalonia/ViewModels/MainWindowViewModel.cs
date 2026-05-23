using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
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
    public static FuncValueConverter<bool, LucideIconKind> PlayingToPlayPauseIconConverter
        => new(x => x ? LucideIconKind.Pause : LucideIconKind.Play);

    public static FuncValueConverter<AudioPlayer?, string> AudioPlayerToDurationTextConverter
        => new(ap =>
        {
            if (ap == null)
                return "0:00";
            return TimeSpan.FromSeconds(ap.Length).ToString(@"m\:ss");
        });
    
    public static FuncValueConverter<AudioPlayer?, float> AudioPlayerToDurationConverter
        => new(ap => ap == null ? 0f : (float)ap.Length);
    
    public static FuncValueConverter<float, string> TimeToCurrentTimeTextConverter
        => new(t => TimeSpan.FromSeconds(t).ToString(@"m\:ss"));
    
    private PASControl pasControl = null!;

    private bool updatingFromTick;
    
    [ObservableProperty]
    public partial float Time { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial double SeekPosition { get; set; }

    [ObservableProperty] 
    public partial AudioPlayer? AudioPlayer { get; set; }

    partial void OnSeekPositionChanged(double value)
    {
        if (AudioPlayer == null)
            return;
        
        if (!updatingFromTick)
        {
            AudioPlayer.Position = value;
            Time = (float)value;
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
        
        AudioPlayer?.Dispose();
        AudioPlayer = AudioPlayer.Load(audioPath);
    }

    [RelayCommand]
    public void OnTick()
    {
        if (AudioPlayer == null)
            return;
        
        updatingFromTick = true;
        SeekPosition = AudioPlayer.Position;
        Time = (float)AudioPlayer.Position;
        IsPlaying = AudioPlayer.Playing;
        updatingFromTick = false;
    }

    [RelayCommand]
    public void PlayPause()
    {
        if (AudioPlayer == null)
            return;
        
        if (AudioPlayer.Playing)
            AudioPlayer.Pause();
        else
            AudioPlayer.Play();
    }

    public void InitializePAS(PASControl control)
    {
        pasControl = control;
    }
}
