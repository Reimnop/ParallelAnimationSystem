using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ShadUI;

namespace ParallelAnimationSystem.Avalonia.Views;

public class PlayPauseAnimation(Control control, TimeSpan? fadeDuration = null)
{
    private CancellationTokenSource? cts;
    private readonly TimeSpan fadeDuration = fadeDuration ?? TimeSpan.FromMilliseconds(500);

    public async Task Trigger()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        var token = cts.Token;

        try
        {
            control.IsVisible = true;
            control.Opacity = 1;
            await FadeOut(token);
            control.IsVisible = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task FadeOut(CancellationToken token) => new Animation
    {
        Duration = fadeDuration,
        FillMode = FillMode.Forward,
        Easing = new EaseOut(),
        Children =
        {
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter(Visual.OpacityProperty, 1d),
                    new Setter(ScaleTransform.ScaleXProperty, 0.5d),
                    new Setter(ScaleTransform.ScaleYProperty, 0.5d)
                }
            },
            new KeyFrame
            {
                Cue = new Cue(1d), 
                Setters =
                {
                    new Setter(Visual.OpacityProperty, 0d),
                    new Setter(ScaleTransform.ScaleXProperty, 1d),
                    new Setter(ScaleTransform.ScaleYProperty, 1d)
                } 
            },
        }
    }.RunAsync(control, token);
}