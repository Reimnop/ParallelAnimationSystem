using System.Numerics;
using System.Runtime.InteropServices;
using SharpFont;
using Tmpx.Common;

namespace Tmpx.Generator;

/// <summary>
/// Extracts curves from glyph outlines, and returns a list of quadratic Bézier curves
/// </summary>
public static class CurveExtractor
{
    private class State
    {
        public required float UnitsPerEm { get; init; }
        
        public List<QuadraticCurve> Curves { get; } = [];
        public Vector2 CurrentPoint { get; set; }
        
    }
    
    public static List<QuadraticCurve> FromOutline(Outline outline, float unitsPerEm)
    {
        var state = new State
        {
            UnitsPerEm = unitsPerEm
        };
        
        var stateGcHandle = GCHandle.Alloc(state);

        try
        {
            var statePtr = GCHandle.ToIntPtr(stateGcHandle);

            var funcs = new OutlineFuncs();
            funcs.MoveFunction = MoveTo;
            funcs.LineFunction = LineTo;
            funcs.ConicFunction = ConicTo;
            funcs.CubicFunction = CubicTo;
            outline.Decompose(funcs, statePtr);

            return state.Curves;
        }
        finally
        {
            stateGcHandle.Free();
        }
    }
    
    private static int MoveTo(ref FTVector to, IntPtr user)
    {
        var state = GetState(user);
        state.CurrentPoint = ToEmSpace(ref to, state.UnitsPerEm);
        return 0;
    }
    
    private static int LineTo(ref FTVector to, IntPtr user)
    {
        var state = GetState(user);
        var endPoint = ToEmSpace(ref to, state.UnitsPerEm);

        var curve = new QuadraticCurve
        {
            P0 = state.CurrentPoint,
            P1 = state.CurrentPoint,
            P2 = endPoint
        };
        state.Curves.Add(curve);
        
        state.CurrentPoint = endPoint;
        return 0;
    }
    
    private static int ConicTo(ref FTVector control, ref FTVector to, IntPtr user)
    {
        var state = GetState(user);
        var controlPoint = ToEmSpace(ref control, state.UnitsPerEm);
        var endPoint = ToEmSpace(ref to, state.UnitsPerEm);

        var curve = new QuadraticCurve
        {
            P0 = state.CurrentPoint,
            P1 = controlPoint,
            P2 = endPoint
        };
        state.Curves.Add(curve);
        
        state.CurrentPoint = endPoint;
        return 0;
    }
    
    
    private static int CubicTo(ref FTVector control1, ref FTVector control2, ref FTVector to, IntPtr user)
    {
        var state = GetState(user);
        var controlPoint1 = ToEmSpace(ref control1, state.UnitsPerEm);
        var controlPoint2 = ToEmSpace(ref control2, state.UnitsPerEm);
        var endPoint = ToEmSpace(ref to, state.UnitsPerEm);

        SplitCubicToQuadratics(state.CurrentPoint, controlPoint1, controlPoint2, endPoint, state.Curves, state.UnitsPerEm);
        
        state.CurrentPoint = endPoint;
        return 0;
    }
    
    private static void SplitCubicToQuadratics(Vector2 p0, Vector2 c1, Vector2 c2, Vector2 p3, List<QuadraticCurve> output, float unitsPerEm, int depth = 0)
    {
        // midpoint subdivision until the cubic is close enough to a quadratic
        var qc = (c1 * 3 - p0 + c2 * 3 - p3) * 0.25f; // approximate
        var error = Vector2.Distance(qc, (c1 + c2) * 0.5f);
    
        if (error < 1.0f / unitsPerEm || depth > 5)
        {
            var curve = new QuadraticCurve
            {
                P0 = p0,
                P1 = qc,
                P2 = p3
            };
            
            output.Add(curve);
        }
        else
        {
            // De Casteljau split at t=0.5
            var m01  = (p0 + c1) * 0.5f;
            var m12  = (c1 + c2) * 0.5f;
            var m23  = (c2 + p3) * 0.5f;
            var m012 = (m01 + m12) * 0.5f;
            var m123 = (m12 + m23) * 0.5f;
            var mid  = (m012 + m123) * 0.5f;
        
            SplitCubicToQuadratics(p0, m01, m012, mid,  output, depth + 1);
            SplitCubicToQuadratics(mid, m123, m23, p3,  output, depth + 1);
        }
    }

    private static State GetState(IntPtr user)
        => (State)GCHandle.FromIntPtr(user).Target!;

    private static Vector2 ToEmSpace(ref FTVector ftVector, float unitsPerEm) 
        => new(ftVector.X.Value / unitsPerEm, ftVector.Y.Value / unitsPerEm);
}