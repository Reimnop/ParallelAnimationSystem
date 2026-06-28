using System;
using Avalonia.Controls;
using Avalonia.Input;
using Lucide.Avalonia;
using ParallelAnimationSystem.Avalonia.Controls;
using ParallelAnimationSystem.Avalonia.ViewModels;

namespace ParallelAnimationSystem.Avalonia.Views;

public partial class PlaybackView : UserControl
{
    private readonly PlayPauseAnimation playPauseAnim;

    private PlaybackViewModel? currentViewModel;
    
    public PlaybackView()
    {
        InitializeComponent();
        
        playPauseAnim = new PlayPauseAnimation(PlayPauseIconContainer);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        currentViewModel?.PlayPauseIconDisplayRequested -= OnPlayPauseIconDisplayRequested;
        
        currentViewModel = DataContext as PlaybackViewModel;
        currentViewModel?.PlayPauseIconDisplayRequested += OnPlayPauseIconDisplayRequested;
    }

    private void OnPlayPauseIconDisplayRequested(object? sender, LucideIconKind e)
    {
        PlayPauseIcon.Kind = e;
        _ = playPauseAnim.Trigger();
    }

    private void TimelineScrubber_OnSeekRequested(object? sender, SeekRequestedEventArgs e)
    {
        currentViewModel?.Seek(e.Position);
    }

    private void PlaybackRootPanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (currentViewModel is null)
            return;

        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            e.Handled = true;
            currentViewModel.PlayPauseDisplayIcon();
        }
    }

    private void PlaybackControlsBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            e.Handled = true;
    }

    private void PlaybackControlsBorder_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
            e.Handled = true;
    }

    private void PlaybackRootPanel_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (currentViewModel is null)
            return;

        if (e.Key == Key.Space)
        {
            e.Handled = true;
            currentViewModel.PlayPauseDisplayIcon();
        }
    }
}