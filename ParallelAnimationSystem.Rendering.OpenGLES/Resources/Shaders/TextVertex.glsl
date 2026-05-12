#version 300 es

precision highp float;

const highp vec2 VERTICES[6] = vec2[](
    vec2(0.0, 1.0),
    vec2(1.0, 1.0),
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(0.0, 0.0),
    vec2(1.0, 1.0)
);

layout(location = 0) in highp vec2 aTransformC0;
layout(location = 1) in highp vec2 aTransformC1;
layout(location = 2) in highp vec2 aTransformC2;
layout(location = 3) in highp vec4 aColor;
layout(location = 4) in highp int aShapeEntryIndex;

// Texture-buffer samplers
uniform highp sampler2D uCurves;
uniform highp isampler2D uCurveIndices;
uniform highp isampler2D uBandEntries;
uniform highp usampler2D uShapeEntries;

uniform highp mat3x2 uMvp;
uniform highp float uZ;
uniform highp vec4  uBaseColor;

// Viewport size for dynamic dilation
uniform highp vec2 uViewportSize;

out highp vec2 vTexCoord;
out highp vec4 vGlyphColor;
flat out highp int vShapeEntryIndex;

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

const int TEX_WIDTH = 2048;

QuadraticCurve getCurve(int index) {
    int tIndex = index * 3; // 3 texels per curve

    ivec2 coord0 = ivec2(tIndex % TEX_WIDTH, tIndex / TEX_WIDTH);
    vec2 p0 = texelFetch(uCurves, coord0, 0).xy;

    ivec2 coord1 = ivec2((tIndex + 1) % TEX_WIDTH, (tIndex + 1) / TEX_WIDTH);
    vec2 p1 = texelFetch(uCurves, coord1, 0).xy;

    ivec2 coord2 = ivec2((tIndex + 2) % TEX_WIDTH, (tIndex + 2) / TEX_WIDTH);
    vec2 p2 = texelFetch(uCurves, coord2, 0).xy;

    return QuadraticCurve(p0, p1, p2);
}

int getCurveIndex(int index) {
    ivec2 coord = ivec2(index % TEX_WIDTH, index / TEX_WIDTH);
    return texelFetch(uCurveIndices, coord, 0).r;
}

BandEntry getBandEntry(int index) {
    ivec2 coord = ivec2(index % TEX_WIDTH, index / TEX_WIDTH);
    ivec2 data = texelFetch(uBandEntries, coord, 0).xy;
    return BandEntry(data.x, data.y);
}

ShapeEntry getShapeEntry(int index) {
    int tIndex = index * 4; // 4 texels per shape entry

    ivec2 coord0 = ivec2(tIndex % TEX_WIDTH, tIndex / TEX_WIDTH);
    uvec4 se0 = texelFetch(uShapeEntries, coord0, 0);

    ivec2 coord1 = ivec2((tIndex + 1) % TEX_WIDTH, (tIndex + 1) / TEX_WIDTH);
    uvec4 se1 = texelFetch(uShapeEntries, coord1, 0);

    ivec2 coord2 = ivec2((tIndex + 2) % TEX_WIDTH, (tIndex + 2) / TEX_WIDTH);
    uvec4 se2 = texelFetch(uShapeEntries, coord2, 0);

    ivec2 coord3 = ivec2((tIndex + 3) % TEX_WIDTH, (tIndex + 3) / TEX_WIDTH);
    uvec4 se3 = texelFetch(uShapeEntries, coord3, 0);

    return ShapeEntry(
        int(se0.x),
        int(se0.y),
        uintBitsToFloat(se0.z),
        uintBitsToFloat(se0.w),
        int(se1.x),
        int(se1.y),
        uintBitsToFloat(se1.z),
        uintBitsToFloat(se1.w),
        uintBitsToFloat(se2.xy),
        uintBitsToFloat(se2.zw),
        se3.x);
}

mat3x2 mult3x2(mat3x2 a, mat3x2 b) {
    return mat3x2(
        a[0] * b[0].x + a[1] * b[0].y,
        a[0] * b[1].x + a[1] * b[1].y,
        a[0] * b[2].x + a[1] * b[2].y + a[2]);
}

mat2 jacobian(mat2 m) {
    float det = determinant(m);
    if (abs(det) < 1.0/65536.0) return mat2(1.0);
    return mat2(m[1][1], -m[0][1], -m[1][0], m[0][0]) / det;
}

void main() {
    vec2 corner = VERTICES[gl_VertexID];
    
    mat3x2 instanceTransform = mat3x2(aTransformC0, aTransformC1, aTransformC2);
    mat3x2 finalMvp = mult3x2(uMvp, instanceTransform);

    vGlyphColor = aColor * uBaseColor;
    vShapeEntryIndex = aShapeEntryIndex;

    if (aShapeEntryIndex >= 0) {
        ShapeEntry se = getShapeEntry(aShapeEntryIndex);

        vec2 localPos = mix(se.min, se.max, corner);
        
        vec2 normal = (corner - vec2(0.5)) * 2.0;
        mat2 linear = mat2(finalMvp[0], finalMvp[1]);
        vec2 clipNormal = normalize(linear * normal);
        vec2 pixelSize = 2.0 / uViewportSize;
        vec2 dilation = clipNormal * pixelSize * 0.5;

        vec2 clipPos = vec2(finalMvp * vec3(localPos, 1.0)) + dilation;

        mat2 j = jacobian(linear);
        vTexCoord = localPos + j * dilation;

        gl_Position = vec4(clipPos, uZ, 1.0);
    } else {
        vec2 clipPos = vec2(finalMvp * vec3(corner, 1.0));
        vTexCoord  = corner;
        gl_Position = vec4(clipPos, uZ, 1.0);
    }
}
