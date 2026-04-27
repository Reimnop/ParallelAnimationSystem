#version 460 core

layout(location = 0) in vec2 aPos;
layout(location = 1) in uint aGlyphIndex;

struct Glyph {
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

layout(std430, binding = 0) readonly buffer GlyphBuffer { Glyph glyphs[]; };
layout(std430, binding = 1) readonly buffer CurveBuffer { QuadraticCurve curves[]; };
layout(std430, binding = 2) readonly buffer CurveIndexBuffer { int curveIndices[]; };
layout(std430, binding = 3) readonly buffer BandEntryBuffer { BandEntry bandEntries[]; };

layout(location = 0) uniform mat3x2 mvp;

out vec2 vTexCoord;
flat out uint vGlyphIndex;

void main() {
    vGlyphIndex = aGlyphIndex;
    
    Glyph glyph = glyphs[aGlyphIndex];

    vec2 corner = vec2(
        float(gl_VertexID & 1),
        float((gl_VertexID >> 1) & 1));

    vec2 localPos = mix(glyph.min, glyph.max, corner);
    vTexCoord = localPos;
    
    vec2 worldPos = mvp * vec3(aPos + localPos, 1.0);
    
    gl_Position = vec4(worldPos, 0.0, 1.0);
}