#version 460 core

layout(location = 0) in vec2 aTransformRow0;
layout(location = 1) in vec2 aTransformRow1;
layout(location = 2) in vec2 aTransformRow2;
layout(location = 3) in uint aShapeIndex;

struct ShapeEntry {
    int horizontalBandEntryBaseIndex;
    int horizontalBandEntryCount;
    float horizontalBandScale;
    float horizontalBandOffset;
    int verticalBandEntryBaseIndex;
    int verticalBandEntryCount;
    float verticalBandScale;
    float verticalBandOffset;
    vec2 min;
    vec2 max;
    uint color;
};

struct QuadraticCurve {
    vec2 p0;
    vec2 p1;
    vec2 p2;
};

struct BandEntry {
    int curveIndexBaseIndex;
    int curveIndexCount;
};

layout(std430, binding = 0) readonly buffer CurveBuffer { QuadraticCurve curves[]; };
layout(std430, binding = 1) readonly buffer CurveIndexBuffer { int curveIndices[]; };
layout(std430, binding = 2) readonly buffer BandEntryBuffer { BandEntry bandEntries[]; };
layout(std430, binding = 3) readonly buffer ShapeEntryBuffer { ShapeEntry shapeEntries[]; };

layout(location = 0) uniform mat3x2 uMvp;

out vec2 vTexCoord;
flat out uint vShapeIndex;

vec2 applyTransform(mat3x2 transform, vec2 pos) {
    return vec2(transform * vec3(pos, 1.0));
}

void main() {
    vShapeIndex = aShapeIndex;
    
    ShapeEntry shapeEntry = shapeEntries[aShapeIndex];

    vec2 corner = vec2(
        float(gl_VertexID & 1),
        float((gl_VertexID >> 1) & 1));

    vec2 localPos = mix(shapeEntry.min, shapeEntry.max, corner);
    vTexCoord = localPos;
    
    mat3x2 transform = mat3x2(
        aTransformRow0,
        aTransformRow1,
        aTransformRow2);
    
    vec2 worldPos = applyTransform(transform, localPos);
    worldPos = applyTransform(uMvp, worldPos);
    
    gl_Position = vec4(worldPos, 0.0, 1.0);
}