using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using Pamx.Common.Implementation;

namespace ParallelAnimationSystem.Core;

public static class VgMigration
{
    public static void MigrateBeatmap(IBeatmap beatmap)
    {
        // Migrate theme color keyframes
        foreach (var o in beatmap.Objects.Concat(beatmap.Prefabs.SelectMany(x => x.BeatmapObjects)))
        {
            if (o.Type == ObjectType.LegacyHelper)
            {
                // Replace all the color events
                for (var i = 0; i < o.ColorEvents.Count; i++)
                {
                    var oldColorKeyframe = o.ColorEvents[i];
                    oldColorKeyframe.Value = new ThemeColor
                    {
                        Index = oldColorKeyframe.Value.Index,
                        EndIndex = oldColorKeyframe.Value.EndIndex,
                        Opacity = 0.35f,
                    };
                    o.ColorEvents[i] = oldColorKeyframe;
                }
            }
        }
        
        // We'll handle camera parenting at migration
        // Start by inserting an empty camera object at the start of the beatmap
        var cameraObject = new BeatmapObject
        {
            StartTime = 0.0f,
            AutoKillType = AutoKillType.NoAutoKill,
            Type = ObjectType.Empty,
            PositionEvents = beatmap.Events.Movement.Select(x => new Keyframe<System.Numerics.Vector2>
            {
                Value = x.Value,
                Time = x.Time,
                Ease = x.Ease,
            }).ToList(),
            ScaleEvents = beatmap.Events.Zoom.Select(x => new Keyframe<System.Numerics.Vector2>
            {
                Value = x.Value / 20.0f * System.Numerics.Vector2.One,
                Time = x.Time,
                Ease = x.Ease,
            }).ToList(),
            // We'll handle rotation separately because camera rotation is absolute rotation
        };
        
        var lastCameraRotation = 0.0f;
        foreach (var rotationEvent in beatmap.Events.Rotation)
        {
            var newRotationEvent = new Keyframe<float>
            {
                Value = rotationEvent.Value - lastCameraRotation,
                Time = rotationEvent.Time,
                Ease = rotationEvent.Ease,
            };
            lastCameraRotation = rotationEvent.Value;
            cameraObject.RotationEvents.Add(newRotationEvent);
        }
        
        // Insert the camera object
        beatmap.Objects.Add(cameraObject);
        
        // Now we'll parent all the camera parent objects to the camera object
        foreach (var o in beatmap.Objects.Concat(beatmap.Prefabs.SelectMany(x => x.BeatmapObjects)))
        {
            if (o.Parent is IIdentifiable<string> { Id: "camera" })
            {
                o.Parent = cameraObject;
                o.ParentType = ParentType.Position | ParentType.Scale | ParentType.Rotation;
                
                // Make sure camera parented objects are above everything else
                o.RenderDepth -= 100;
            }
        }
    }
}