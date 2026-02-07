namespace ParallelAnimationSystem.Core.Animation;

public delegate T Interpolator<T>(T from, T to, float time);