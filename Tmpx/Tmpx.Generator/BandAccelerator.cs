using System.Numerics;
using Tmpx.Common;

namespace Tmpx.Generator;

/// <summary>
/// Categorizes curves into horizontal and vertical bands
/// </summary>
public static class BandAccelerator
{
    public static void Process(
        IReadOnlyList<QuadraticCurve> curves,
        Vector2 min, Vector2 max,
        int bandCount, 
        out List<int> curveIndices, out List<BandEntry> horizontalBandEntries, out List<BandEntry> verticalBandEntries)
    {
        var horizontalBands = Enumerable.Range(0, bandCount).Select(_ => new List<int>()).ToArray();
        var verticalBands = Enumerable.Range(0, bandCount).Select(_ => new List<int>()).ToArray();

        var size = max - min;

        for (var i = 0; i < curves.Count; i++)
        {
            var c = curves[i];
            
            // curve's Y extent -> which horizontal bands does it touch?
            var yLo = MathF.Min(MathF.Min(c.P0.Y, c.P1.Y), c.P2.Y);
            var yHi = MathF.Max(MathF.Max(c.P0.Y, c.P1.Y), c.P2.Y);
            var bandYLo = Math.Clamp((int)((yLo - min.Y) / size.Y * bandCount), 0, bandCount - 1);
            var bandYHi = Math.Clamp((int)((yHi - min.Y) / size.Y * bandCount), 0, bandCount - 1);
            for (var b = bandYLo; b <= bandYHi; b++)
                horizontalBands[b].Add(i);
            
            // curve's X extent -> which vertical bands does it touch?
            var xLo = MathF.Min(MathF.Min(c.P0.X, c.P1.X), c.P2.X);
            var xHi = MathF.Max(MathF.Max(c.P0.X, c.P1.X), c.P2.X);
            var bandXLo = Math.Clamp((int)((xLo - min.X) / size.X * bandCount), 0, bandCount - 1);
            var bandXHi = Math.Clamp((int)((xHi - min.X) / size.X * bandCount), 0, bandCount - 1);
            for (var b = bandXLo; b <= bandXHi; b++)
                verticalBands[b].Add(i);
        }
        
        // sort each horizontal band by descending max-X for shader early-out
        foreach (var band in horizontalBands)
            band.Sort((a, b) => MathF.Max(MathF.Max(curves[b].P0.X, curves[b].P1.X), curves[b].P2.X)
                .CompareTo(MathF.Max(MathF.Max(curves[a].P0.X, curves[a].P1.X), curves[a].P2.X)));
        
        // sort each vertical band by descending max-Y
        foreach (var band in verticalBands)
            band.Sort((a, b) => MathF.Max(MathF.Max(curves[b].P0.Y, curves[b].P1.Y), curves[b].P2.Y)
                .CompareTo(MathF.Max(MathF.Max(curves[a].P0.Y, curves[a].P1.Y), curves[a].P2.Y)));
        
        curveIndices = [];
        horizontalBandEntries = [];
        verticalBandEntries = [];
        
        foreach (var band in horizontalBands)
            horizontalBandEntries.Add(AddBand(band, curveIndices));
        
        foreach (var band in verticalBands)
            verticalBandEntries.Add(AddBand(band, curveIndices));
    }

    private static BandEntry AddBand(List<int> band, List<int> curveIndices)
    {
        var baseIndex = curveIndices.Count;
        curveIndices.AddRange(band);
        return new BandEntry
        {
            CurveIndexBaseIndex = baseIndex,
            CurveIndexCount = band.Count
        };
    }
}