using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Avalonia.Integration;
using ParallelAnimationSystem.Core.Service;
using Window = ShadUI.Window;

namespace ParallelAnimationSystem.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    public static FuncValueConverter<bool, LucideIconKind> PlayingToPlayPauseIconConverter
        => new(x => x ? LucideIconKind.Pause : LucideIconKind.Play);

    public static FuncValueConverter<AudioPlayer?, string> AudioPlayerToDurationTextConverter
        => new(ap => ap == null ? "0:00" : TimeSpan.FromSeconds(ap.Length).ToString(@"m\:ss"));
    
    public static FuncValueConverter<AudioPlayer?, double> AudioPlayerToDurationConverter
        => new(ap => ap?.Length ?? 0f);
    
    public static FuncValueConverter<double, string> TimeToCurrentTimeTextConverter
        => new(t => TimeSpan.FromSeconds(t).ToString(@"m\:ss"));
    
    public static FuncValueConverter<int, string> FpsTextConverter
        => new(x => $"{x} FPS");

    [ObservableProperty] 
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial double ScrubberPosition { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty] 
    public partial AudioPlayer? AudioPlayer { get; set; }

    public Func<float> GetTimeCallback => TickGetTime;

    private readonly DispatcherTimer fpsTimer;
    
    private PASControl pasControl = null!;
    private int countingFps;

    public MainWindowViewModel()
    {
        fpsTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        fpsTimer.Tick += FpsTimerOnTick;
        fpsTimer.Start();
    }

    public void Dispose()
    {
        fpsTimer.Stop();
        AudioPlayer?.Dispose();
    }
    
    private void FpsTimerOnTick(object? sender, EventArgs e)
    {
        Fps = countingFps;
        countingFps = 0;
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
    public void PlayPause()
    {
        if (AudioPlayer == null)
            return;
        
        if (AudioPlayer.Playing)
            AudioPlayer.Pause();
        else
            AudioPlayer.Play();
    }
    
    public float TickGetTime()
    {
        var time = (float?)AudioPlayer?.Position ?? 0f;
        ScrubberPosition = time;
        IsPlaying = AudioPlayer?.Playing ?? false;
        countingFps++;
        return time;
    }

    public void InitializePAS(PASControl control)
    {
        pasControl = control;
    }

    public void Seek(double position)
    {
        if (AudioPlayer == null)
            return;
        
        AudioPlayer.Position = position;
    }
}
