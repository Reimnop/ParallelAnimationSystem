namespace ParallelAnimationSystem.Core.Service;

public delegate T Interpolator<T>(T from, T to, float time);