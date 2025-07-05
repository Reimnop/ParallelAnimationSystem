using OpenTK.Mathematics;
using Pamx.Common;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Animation;

namespace ParallelAnimationSystem.Core;

public class AnimationRunner
{
    public IReadOnlySet<GameObject> AliveGameObjects => aliveGameObjects;
    public Vector2 CameraPosition { get; private set; }
    public float CameraScale { get; private set; }
    public float CameraRotation { get; private set; }
    public BloomData Bloom { get; private set; }
    public float Hue { get; private set; }
    public LensDistortionData LensDistortion { get; private set; }
    public float ChromaticAberration { get; private set; }
    public Data.VignetteData Vignette { get; private set; }
    public Data.GradientData Gradient { get; private set; }
    public GlitchData Glitch { get; private set; }
    public float Shake { get; private set; }
    public Color4<Rgba> BackgroundColor { get; private set; }

    public event EventHandler<GameObject>? ObjectSpawned;
    public event EventHandler<GameObject>? ObjectKilled;
    
    public int ObjectCount => startTimeSortedGameObjects.Count;

    private readonly List<GameObject> startTimeSortedGameObjects = [];
    private readonly List<GameObject> killTimeSortedGameObjects = [];
    private int spawnIndex;
    private int killIndex;
    private readonly SortedSet<GameObject> aliveGameObjects = new(new GameObjectDepthComparer());
    
    private readonly Sequence<ITheme, ThemeColors> themeColorSequence;
    private readonly Sequence<Vector2, Vector2> cameraPositionAnimation;
    private readonly Sequence<float, float> cameraScaleAnimation;
    private readonly Sequence<float, float> cameraRotationAnimation;

    private readonly Sequence<BloomData, BloomData> bloomSequence;
    private readonly Sequence<float, float> hueSequence;
    private readonly Sequence<LensDistortionData, LensDistortionData> lensDistortionSequence;
    private readonly Sequence<float, float> chromaticAberrationSequence;
    private readonly Sequence<VignetteData, Data.VignetteData> vignetteSequence;
    private readonly Sequence<GradientData, Data.GradientData> gradientSequence;
    private readonly Sequence<GlitchData, GlitchData> glitchSequence;
    private readonly Sequence<float, float> shakeSequence;
        
    private float lastTime;
    
    public AnimationRunner(
        IEnumerable<GameObject> gameObjects,
        Sequence<ITheme, ThemeColors> themeColorSequence,
        Sequence<Vector2, Vector2> cameraPositionAnimation,
        Sequence<float, float> cameraScaleAnimation,
        Sequence<float, float> cameraRotationAnimation,
        Sequence<BloomData, BloomData> bloomSequence,
        Sequence<float, float> hueSequence,
        Sequence<LensDistortionData, LensDistortionData> lensDistortionSequence,
        Sequence<float, float> chromaticAberrationSequence,
        Sequence<VignetteData, Data.VignetteData> vignetteSequence,
        Sequence<GradientData, Data.GradientData> gradientSequence,
        Sequence<GlitchData, GlitchData> glitchSequence,
        Sequence<float, float> shakeSequence)
    {
        // Set sequences
        this.themeColorSequence = themeColorSequence;
        this.cameraPositionAnimation = cameraPositionAnimation;
        this.cameraScaleAnimation = cameraScaleAnimation;
        this.cameraRotationAnimation = cameraRotationAnimation;
        this.bloomSequence = bloomSequence;
        this.hueSequence = hueSequence;
        this.lensDistortionSequence = lensDistortionSequence;
        this.chromaticAberrationSequence = chromaticAberrationSequence;
        this.vignetteSequence = vignetteSequence;
        this.gradientSequence = gradientSequence;
        this.glitchSequence = glitchSequence;
        this.shakeSequence = shakeSequence;

        foreach (var gameObject in gameObjects)
        {
            startTimeSortedGameObjects.Add(gameObject);
            killTimeSortedGameObjects.Add(gameObject);
        }
        
        // Sort them
        startTimeSortedGameObjects.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));
        killTimeSortedGameObjects.Sort((x, y) => x.KillTime.CompareTo(y.KillTime));
    }

    public void Process(float time, int workers = -1)
    {
        // Update theme
        var themeColors = themeColorSequence.Interpolate(time);
        if (themeColors is null)
            throw new InvalidOperationException("Theme colors are null, theme color sequence might not be populated");
        
        BackgroundColor = themeColors.Background;
        
        // Spawn and kill objects according to time
        SpawnAndKillObjects(time);
        
        // Update sequences
        CameraPosition = cameraPositionAnimation.Interpolate(time, themeColors);
        CameraScale = cameraScaleAnimation.Interpolate(time, themeColors);
        CameraRotation = cameraRotationAnimation.Interpolate(time, themeColors);
        Bloom = bloomSequence.Interpolate(time, themeColors);
        Hue = hueSequence.Interpolate(time, themeColors);
        LensDistortion = lensDistortionSequence.Interpolate(time, themeColors);
        ChromaticAberration = chromaticAberrationSequence.Interpolate(time, themeColors);
        Vignette = vignetteSequence.Interpolate(time, themeColors);
        Gradient = gradientSequence.Interpolate(time, themeColors);
        Glitch = glitchSequence.Interpolate(time, themeColors);
        Shake = shakeSequence.Interpolate(time, themeColors);
        
        // Update all objects in parallel
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = workers
        };
        Parallel.ForEach(aliveGameObjects, parallelOptions, (x, _) => ProcessGameObjectAsync(x, themeColors, time));
    }

    private static void ProcessGameObjectAsync(GameObject gameObject, ThemeColors themeColors, float time)
    {
        gameObject.CalculateTransform(time);
        gameObject.CalculateThemeColor(time, themeColors);
    }

    private void SpawnAndKillObjects(float time)
    {
        if (time > lastTime)
        {
            // Update objects forward in time
            // Spawn
            while (spawnIndex < startTimeSortedGameObjects.Count && time >= startTimeSortedGameObjects[spawnIndex].StartTime)
            {
                var obj = startTimeSortedGameObjects[spawnIndex];
                aliveGameObjects.Add(obj);
                spawnIndex++;
                ObjectSpawned?.Invoke(this, obj);
            }

            // Kill
            while (killIndex < killTimeSortedGameObjects.Count && time >= killTimeSortedGameObjects[killIndex].KillTime)
            {
                var obj = killTimeSortedGameObjects[killIndex];
                aliveGameObjects.Remove(obj);
                killIndex++;
                ObjectKilled?.Invoke(this, obj);
            }
        }
        else if (time < lastTime)
        {
            // Update objects backwards in time
            // Spawn
            while (killIndex - 1 >= 0 && time < killTimeSortedGameObjects[killIndex - 1].KillTime)
            {
                var obj = killTimeSortedGameObjects[killIndex - 1];
                aliveGameObjects.Add(obj);
                killIndex--;
                ObjectSpawned?.Invoke(this, obj);
            }
            
            // Kill
            while (spawnIndex - 1 >= 0 && time < startTimeSortedGameObjects[spawnIndex - 1].StartTime)
            {
                var obj = startTimeSortedGameObjects[spawnIndex - 1];
                aliveGameObjects.Remove(obj);
                spawnIndex--;
                ObjectKilled?.Invoke(this, obj);
            }
        }
        
        // Update last time
        lastTime = time;
    }
}