using System.Numerics;

namespace ParallelAnimationSystem.Core.Shape;

// code "borrowed" from Project Arrhythmia
// thanks Pidge for the "generous" "donation"
// (pls don't sue me)
public static class VGShape
{
    public const int MaxVertexCount = 32;
    public const int MinVertexCount = 3;
    public const int SegmentsPerCorner = 4; // Adjust for smoother/rougher corners

    public static VGMesh GenerateMesh(VGShapeInfo shapeInfo)
    {
        var radius = shapeInfo.Sides switch
        {
            3 => 0.575f,
            4 => 0.7071f,
            6 => 0.5f,
            _ => 0.5f
        };
        return GenerateRoundedRingMesh(radius, shapeInfo.Sides, shapeInfo.Roundness, shapeInfo.Thickness, shapeInfo.SliceCount);
    }
    
    public static VGMesh GenerateFilledMesh(float radius, int vertexCount)
    {
        vertexCount = Math.Clamp(vertexCount, MinVertexCount, MaxVertexCount);

        // Generate vertices
        var vertices = new Vector2[vertexCount + 1]; // +1 for center point
        vertices[0] = Vector2.Zero; // Center vertex

        // Generate outer vertices
        var angleStep = (2f * MathF.PI) / vertexCount;
        var startAngle = -MathF.PI / 2f; // -90 degrees in radians

        if (vertexCount == 4)
        {
            // make the square not look like a diamond
            startAngle += angleStep / 2f;
        }
        else if (vertexCount % 2 == 1)
        {
            startAngle += angleStep / 2f;
        }

        for (var i = 0; i < vertexCount; i++)
        {
            var angle = startAngle + i * angleStep;
            var x = MathF.Cos(angle) * radius;
            var y = MathF.Sin(angle) * radius;
            vertices[i + 1] = new Vector2(x, y);
        }

        // Generate indices
        var indices = new int[vertexCount * 3];
        for (var i = 0; i < vertexCount; i++)
        {
            var indexOffset = i * 3;
            indices[indexOffset] = 0; // Center
            indices[indexOffset + 1] = (i + 2 > vertexCount) ? 1 : i + 2;
            indices[indexOffset + 2] = i + 1;
        }

        return new VGMesh(vertices, indices);
    }
    
    public static VGMesh GenerateRoundedPolygonMesh(float radius, int cornerCount, float cornerRoundness)
    {
        cornerCount = Math.Clamp(cornerCount, MinVertexCount, MaxVertexCount);
        
        cornerRoundness = Math.Clamp(cornerRoundness, 0f, 1f);
        var totalVertices = cornerCount * (SegmentsPerCorner + 1);

        // Generate base corner positions
        var cornerPositions = new Vector2[cornerCount];
        var angleStep = (2f * MathF.PI) / cornerCount;
        var startAngle = -MathF.PI / 2f + (cornerCount == 4 || cornerCount % 2 == 1 ? angleStep / 2 : 0);

        for (var i = 0; i < cornerCount; i++)
        {
            var angle = startAngle + i * angleStep;
            cornerPositions[i] = new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius);
        }

        // Generate rounded corners
        var vertices = new Vector2[totalVertices];
        var currentVertex = 0;

        for (var i = 0; i < cornerCount; i++)
        {
            var corner = cornerPositions[i];
            var prevCorner = cornerPositions[(i - 1 + cornerCount) % cornerCount];
            var nextCorner = cornerPositions[(i + 1) % cornerCount];

            // Calculate control points for rounded corner
            var toPrev = Vector2.Normalize(prevCorner - corner) * (radius * cornerRoundness);
            var toNext = Vector2.Normalize(nextCorner - corner) * (radius * cornerRoundness);
            var p1 = corner + toPrev; // Changed minus to plus
            var p2 = corner;
            var p3 = corner + toNext; // Changed minus to plus

            // Generate points along the rounded corner
            for (var j = 0; j <= SegmentsPerCorner; j++)
            {
                var t = j / (float)SegmentsPerCorner;
                vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
            }
        }

        // Generate indices
        var finalVertices = new Vector2[totalVertices + 1];
        finalVertices[0] = Vector2.Zero; // Center point
        for (var i = 0; i < totalVertices; i++)
        {
            finalVertices[i + 1] = vertices[i];
        }

        var indices = new int[totalVertices * 3];

        // Create triangle fan from center
        for (var i = 0; i < totalVertices; i++)
        {
            var indexOffset = i * 3;
            indices[indexOffset] = 0; // Center vertex
            indices[indexOffset + 1] = (i + 2 > totalVertices) ? 1 : i + 2;
            indices[indexOffset + 2] = i + 1;
        }

        return new VGMesh(finalVertices, indices);
    }

    public static VGMesh GenerateRingMesh(float radius, int vertexCount, float thickness)
    {
        if (thickness >= 1f)
            return GenerateFilledMesh(radius, vertexCount);

        // Minimum 3 vertices for a circle
        vertexCount = Math.Clamp(vertexCount, MinVertexCount, MaxVertexCount);

        // Generate vertices
        var vertices = new Vector2[vertexCount * 2]; // +1 for center point

        // Generate outer vertices
        var angleStep = (2f * MathF.PI) / vertexCount;
        var startAngle = -MathF.PI / 2f; // -90 degrees in radians

        if (vertexCount == 4)
        {
            // make the square not look like a diamond
            startAngle += angleStep / 2f;
        }
        else if (vertexCount % 2 == 1)
        {
            startAngle += angleStep / 2f;
        }

        for (var i = 0; i < vertexCount; i++)
        {
            var angle = startAngle + i * angleStep;
            var x = MathF.Cos(angle) * radius;
            var y = MathF.Sin(angle) * radius;
            vertices[i] = new Vector2(x, y);
            vertices[i + vertexCount] = new Vector2(x * (1f - thickness), y * (1f - thickness));
        }

        // Generate triangles
        var indices = new int[vertexCount * 6];
        for (var i = 0; i < vertexCount; i++)
        {
            var indexOffset = i * 6;
            var next = (i + 1) % vertexCount;
            indices[indexOffset] = i;
            indices[indexOffset + 1] = i + vertexCount;
            indices[indexOffset + 2] = next;

            indices[indexOffset + 3] = next;
            indices[indexOffset + 4] = i + vertexCount;
            indices[indexOffset + 5] = next + vertexCount;
        }

        return new VGMesh(vertices, indices);
    }

    public static VGMesh GenerateRoundedRingMesh(float radius, int cornerCount, float cornerRoundness, float thickness)
    {
        if (thickness >= 1)
            return GenerateRoundedPolygonMesh(radius, cornerCount, cornerRoundness);

        if (cornerRoundness <= 0)
            return GenerateRingMesh(radius, cornerCount, thickness);
        
        var verticesPerRing = cornerCount * (SegmentsPerCorner + 1);
        var totalVertices = verticesPerRing * 2;

        // Generate base corner positions for outer and inner rings
        var outerCorners = new Vector2[cornerCount];
        var innerCorners = new Vector2[cornerCount];
        var angleStep = (2f * MathF.PI) / cornerCount;
        var startAngle = -MathF.PI / 2f + (cornerCount == 4 || cornerCount % 2 == 1 ? angleStep / 2 : 0);

        for (var i = 0; i < cornerCount; i++)
        {
            var angle = startAngle + i * angleStep;
            outerCorners[i] = new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius);
            innerCorners[i] = outerCorners[i] * (1 - thickness);
        }

        // Generate vertices for both rings
        var vertices = new Vector2[totalVertices];
        var currentVertex = 0;

        // Generate outer ring vertices
        for (var i = 0; i < cornerCount; i++)
        {
            var corner = outerCorners[i];
            var prevCorner = outerCorners[(i - 1 + cornerCount) % cornerCount];
            var nextCorner = outerCorners[(i + 1) % cornerCount];

            var toPrev = Vector2.Normalize(prevCorner - corner) * (radius * cornerRoundness);
            var toNext = Vector2.Normalize(nextCorner - corner) * (radius * cornerRoundness);
            var p1 = corner + toPrev;
            var p2 = corner;
            var p3 = corner + toNext;

            for (var j = 0; j <= SegmentsPerCorner; j++)
            {
                var t = j / (float)SegmentsPerCorner;
                vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
            }
        }

        var insideRadius = radius * (1 - thickness) * (cornerRoundness * (1 - thickness));

        // Generate inner ring vertices
        for (var i = 0; i < cornerCount; i++)
        {
            var corner = innerCorners[i];
            var prevCorner = innerCorners[(i - 1 + cornerCount) % cornerCount];
            var nextCorner = innerCorners[(i + 1) % cornerCount];

            var toPrev = Vector2.Normalize(prevCorner - corner) * insideRadius;
            var toNext = Vector2.Normalize(nextCorner - corner) * insideRadius;
            var p1 = corner + toPrev;
            var p2 = corner;
            var p3 = corner + toNext;

            for (var j = 0; j <= SegmentsPerCorner; j++)
            {
                var t = j / (float)SegmentsPerCorner;
                vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
            }
        }

        // Generate indices connecting inner and outer rings
        var indices = new int[verticesPerRing * 6];

        for (var i = 0; i < verticesPerRing; i++)
        {
            var indexOffset = i * 6;
            
            var next = (i + 1) % verticesPerRing;

            // First triangle
            indices[indexOffset] = i;
            indices[indexOffset + 1] = i + verticesPerRing;
            indices[indexOffset + 2] = next;

            // Second triangle
            indices[indexOffset + 3] = next;
            indices[indexOffset + 4] = i + verticesPerRing;
            indices[indexOffset + 5] = next + verticesPerRing;
        }

        return new VGMesh(vertices, indices);
    }

    public static VGMesh GenerateRoundedRingMesh(
        float radius,
        int cornerCount,
        float cornerRoundness,
        float thickness,
        int sliceCount) // -1 means draw full shape
    {
        cornerCount = Math.Clamp(cornerCount, MinVertexCount, MaxVertexCount);
        sliceCount = sliceCount < 0 ? cornerCount : Math.Clamp(sliceCount, 1, cornerCount);

        if (cornerCount > 12)
            cornerRoundness = 0;
        else
        {
            var pos = float.Lerp(0.5f, 0.25f, (cornerCount - 3f) / 9f);
            cornerRoundness = float.Lerp(0, pos, Math.Clamp(cornerRoundness, 0f, 1f));
        }

        thickness = Math.Clamp(thickness, 0f, 1f);

        if (thickness >= 1 && cornerCount == sliceCount)
            return GenerateRoundedPolygonMesh(radius, cornerCount, cornerRoundness);

        if (cornerCount == sliceCount)
            return GenerateRoundedRingMesh(radius, cornerCount, cornerRoundness, thickness);
        
        var verticesPerRing = 0;

        for (var i = 0; i < sliceCount; i++)
        {
            if (i == 0 && cornerCount != sliceCount)
            {
                verticesPerRing += 1;
            }
            else
            {
                verticesPerRing += SegmentsPerCorner + 1;
            }
        }

        verticesPerRing += 1; // +1 for end cap

        var totalVertices = verticesPerRing * 2;

        // Generate base corner positions for outer and inner rings
        var outerCorners = new Vector2[sliceCount + 1]; // +1 for end position
        var innerCorners = new Vector2[sliceCount + 1];
        var angleStep = (2f * MathF.PI) / cornerCount;
        var startAngle = -MathF.PI / 2f + (cornerCount == 4 || cornerCount % 2 == 1 ? angleStep / 2 : 0);

        for (var i = 0; i <= sliceCount; i++)
        {
            var angle = startAngle + i * angleStep;
            outerCorners[i] = new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius);
            innerCorners[i] = outerCorners[i] * (1 - thickness);
        }

        // Generate vertices for both rings
        var vertices = new Vector2[totalVertices];
        var currentVertex = 0;

        // Generate outer ring vertices
        for (var i = 0; i < sliceCount; i++)
        {
            var corner = outerCorners[i];

            if (i == 0 && cornerCount != sliceCount)
            {
                vertices[currentVertex++] = corner;
            }
            else
            {
                var prevCorner = i == 0 ? corner : outerCorners[i - 1];
                var nextCorner = outerCorners[i + 1];

                var toPrev = Vector2.Normalize(prevCorner - corner) * (radius * cornerRoundness);
                var toNext = Vector2.Normalize(nextCorner - corner) * (radius * cornerRoundness);
                var p1 = corner + toPrev;
                var p2 = corner;
                var p3 = corner + toNext;

                for (var j = 0; j <= SegmentsPerCorner; j++)
                {
                    var t = j / (float)SegmentsPerCorner;
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                }
            }
        }

        // Add final vertex for end cap
        vertices[currentVertex++] = outerCorners[sliceCount];

        var insideRadius = radius * (1 - thickness) * (cornerRoundness * (1 - thickness));

        // Generate inner ring vertices (same pattern as outer)
        for (var i = 0; i < sliceCount; i++)
        {
            var corner = innerCorners[i];

            if (i == 0 && cornerCount != sliceCount)
            {
                vertices[currentVertex++] = corner;
            }
            else
            {
                var prevCorner = i == 0 ? corner : innerCorners[i - 1];
                var nextCorner = innerCorners[i + 1];

                var toPrev = Vector2.Normalize(prevCorner - corner) * insideRadius;
                var toNext = Vector2.Normalize(nextCorner - corner) * insideRadius;
                var p1 = corner + toPrev;
                var p2 = corner;
                var p3 = corner + toNext;

                for (var j = 0; j <= SegmentsPerCorner; j++)
                {
                    var t = j / (float)SegmentsPerCorner;
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                }
            }
        }

        // Add final vertex for inner end cap
        vertices[currentVertex] = innerCorners[sliceCount];

        // Generate triangles connecting inner and outer rings
        var indices = new int[(verticesPerRing - 1) * 6];

        for (var i = 0; i < verticesPerRing - 1; i++)
        {
            var indexOffset = i * 6;
            var next = i + 1;

            // First triangle
            indices[indexOffset] = i;
            indices[indexOffset + 1] = i + verticesPerRing;
            indices[indexOffset + 2] = next;

            // Second triangle
            indices[indexOffset + 3] = next;
            indices[indexOffset + 4] = i + verticesPerRing;
            indices[indexOffset + 5] = next + verticesPerRing;
        }

        return new VGMesh(vertices, indices);
    }

    private static Vector2 QuadraticBezier(Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var u = 1f - t;
        return u * u * p1 + 2f * u * t * p2 + t * t * p3;
    }
}