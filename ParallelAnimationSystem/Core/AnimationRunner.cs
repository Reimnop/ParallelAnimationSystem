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
    public Color4<Rgba> BackgroundColor { get; private set; }
    
    public int ObjectCount => startTimeSortedGameObjects.Count;

    private readonly List<GameObject> startTimeSortedGameObjects;
    private readonly List<GameObject> killTimeSortedGameObjects;
    private int spawnIndex;
    private int killIndex;
    private readonly HashSet<GameObject> aliveGameObjects = [];
    
    private readonly Sequence<ITheme, ThemeColors> themeColorSequence;
    private readonly Sequence<Vector2, Vector2> cameraPositionAnimation;
    private readonly Sequence<float, float> cameraScaleAnimation;
    private readonly Sequence<float, float> cameraRotationAnimation;

    private readonly Sequence<BloomData, BloomData> bloomSequence;
    private readonly Sequence<float, float> hueSequence;
        
    private float lastTime;
    
    public AnimationRunner(
        IEnumerable<GameObject> gameObjects, 
        Sequence<ITheme, ThemeColors> themeColorSequence,
        Sequence<Vector2, Vector2> cameraPositionAnimation,
        Sequence<float, float> cameraScaleAnimation,
        Sequence<float, float> cameraRotationAnimation,
        Sequence<BloomData, BloomData> bloomSequence,
        Sequence<float, float> hueSequence)
    {
        // Set sequences
        this.themeColorSequence = themeColorSequence;
        this.cameraPositionAnimation = cameraPositionAnimation;
        this.cameraScaleAnimation = cameraScaleAnimation;
        this.cameraRotationAnimation = cameraRotationAnimation;
        this.bloomSequence = bloomSequence;
        this.hueSequence = hueSequence;
        
        // A bit expensive, but we have to copy the enumerable to a list
        // to prevent multiple enumeration
        var goList = gameObjects.ToList();
        startTimeSortedGameObjects = goList.ToList();
        killTimeSortedGameObjects = goList.ToList();
        
        // Sort them
        startTimeSortedGameObjects.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));
        killTimeSortedGameObjects.Sort((x, y) => x.KillTime.CompareTo(y.KillTime));
    }

    public void Process(float time, int workers = -1)
    {
        // Update sequences
        CameraPosition = cameraPositionAnimation.Interpolate(time);
        CameraScale = cameraScaleAnimation.Interpolate(time);
        CameraRotation = cameraRotationAnimation.Interpolate(time);
        Bloom = bloomSequence.Interpolate(time);
        Hue = hueSequence.Interpolate(time);
        
        // Spawn and kill objects according to time
        SpawnAndKillObjects(time);
        
        // Update theme
        var themeColors = themeColorSequence.Interpolate(time);
        if (themeColors is null)
            throw new InvalidOperationException("Theme colors are null, theme color sequence might not be populated");

        BackgroundColor = themeColors.Background;
        
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
                aliveGameObjects.Add(startTimeSortedGameObjects[spawnIndex]);
                spawnIndex++;
            }

            // Despawn
            while (killIndex < killTimeSortedGameObjects.Count && time >= killTimeSortedGameObjects[killIndex].KillTime)
            {
                aliveGameObjects.Remove(killTimeSortedGameObjects[killIndex]);
                killIndex++;
            }
        }
        else if (time < lastTime)
        {
            // Update objects backwards in time
            // Spawn
            while (spawnIndex - 1 >= 0 && time < startTimeSortedGameObjects[spawnIndex - 1].StartTime)
            {
                aliveGameObjects.Remove(startTimeSortedGameObjects[spawnIndex - 1]);
                spawnIndex--;
            }
            
            // Despawn
            while (killIndex - 1 >= 0 && time < killTimeSortedGameObjects[killIndex - 1].KillTime)
            {
                aliveGameObjects.Add(killTimeSortedGameObjects[killIndex - 1]);
                killIndex--;
            }
        }
        
        // Update last time
        lastTime = time;
    }
}