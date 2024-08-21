namespace ParallelAnimationSystem.Core.Animation;

public delegate TOut Interpolator<in TIn, out TOut>(TIn a, TIn b, float t, object? context);