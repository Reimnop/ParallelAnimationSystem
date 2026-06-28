using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using ParallelAnimationSystem.Avalonia.Integration;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

[ViewModelView<PlaybackView>]
public partial class PlaybackViewModel : ViewModelBase, IDisposable
{
    public static FuncValueConverter<bool, LucideIconKind> PlayingToPlayPauseIconConverter
        => new(x => x ? LucideIconKind.Pause : LucideIconKind.Play);

    public static FuncValueConverter<AudioPlayer?, string> AudioPlayerToDurationTextConverter
        => new(ap => ap == null ? "0:00" : TimeSpan.FromSeconds(ap.Length).ToString(@"m\:ss"));
    
    public static FuncValueConverter<AudioPlayer?, double> AudioPlayerToDurationConverter
        => new(ap => ap?.Length ?? 0f);
    
    public static FuncValueConverter<double, string> TimeToCurrentTimeTextConverter
        => new(t => TimeSpan.FromSeconds(t).ToString(@"m\:ss"));

    public event EventHandler<LucideIconKind>? PlayPauseIconDisplayRequested;

    [ObservableProperty]
    public partial PASViewModel PASViewModel { get; set; }
    
    [ObservableProperty] 
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial double ScrubberPosition { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty] 
    public partial AudioPlayer? AudioPlayer { get; set; }

    private readonly DispatcherTimer fpsTimer;
    private int countingFps;

    public PlaybackViewModel(PASViewModel pasViewModel)
    {
        PASViewModel = pasViewModel;
        
        PASViewModel = pasViewModel;
        PASViewModel.TickCallback = OnTick;
        
        fpsTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        
        fpsTimer.Tick += (_, _) =>
        {
            Fps = countingFps;
            countingFps = 0;
        };
        
        fpsTimer.Start();
    }

    public void Dispose()
    {
        PASViewModel.Dispose();
        AudioPlayer?.Dispose();
        fpsTimer.Stop();
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
        
        PASViewModel.BeatmapService.LoadBeatmapFromPath(beatmapPath);
        
        AudioPlayer?.Dispose();
        AudioPlayer = AudioPlayer.Load(audioPath);
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

    public void PlayPauseDisplayIcon()
    {
        PlayPauseIconDisplayRequested?.Invoke(this, IsPlaying ? LucideIconKind.Pause : LucideIconKind.Play);
        PlayPause();
    }
    
    public float OnTick()
    {
        var time = (float?)AudioPlayer?.Position ?? 0f;
        ScrubberPosition = time;
        IsPlaying = AudioPlayer?.Playing ?? false;
        countingFps++;
        return time;
    }

    public void Seek(double position)
    {
        if (AudioPlayer == null)
            return;
        
        AudioPlayer.Position = position;
    }
}