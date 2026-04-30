#version 460 core

const vec2 CORNERS[] = vec2[](
    vec2(0.0, 0.0),
    vec2(0.0, 1.0),
    vec2(1.0, 0.0),
    vec2(1.0, 1.0)
);

const vec2 NORMALS[] = vec2[](
    vec2(-1.0, -1.0),
    vec2(-1.0,  1.0),
    vec2( 1.0, -1.0),
    vec2( 1.0,  1.0)
);

layout(location = 0) in vec2 aTransformRow0;
layout(location = 1) in vec2 aTransformRow1;
layout(location = 2) in vec2 aTransformRow2;
layout(location = 3) in uint aColor;
layout(location = 4) in int aShapeIndex;

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
layout(location = 1) uniform vec2 uViewportSize;

out vec2 vTexCoord;
out vec4 vColor;
flat out int vShapeIndex;

mat3x2 mult3x2(mat3x2 a, mat3x2 b) {
    return mat3x2(
        a[0] * b[0].x + a[1] * b[0].y,
        a[0] * b[1].x + a[1] * b[1].y,
        a[0] * b[2].x + a[1] * b[2].y + a[2]);
}

mat2 jacobian(mat2 m) {
    float det = determinant(m);
    
    if (abs(det) < 1.0/65536.0) {
        return mat2(1.0);
    }
    
    mat2 j = mat2(
         m[1][1], -m[0][1],
        -m[1][0],  m[0][0]) / det;
    return j;
}

void main() {
    vShapeIndex = aShapeIndex;
    vColor = vec4(
        float((aColor       ) & 0xFFu) / 255.0,
        float((aColor >>  8u) & 0xFFu) / 255.0,
        float((aColor >> 16u) & 0xFFu) / 255.0,
        float((aColor >> 24u) & 0xFFu) / 255.0);

    vec2 corner = CORNERS[gl_VertexID % 4];
    vec2 normal = NORMALS[gl_VertexID % 4];

    mat3x2 transform = mat3x2(
        aTransformRow0,
        aTransformRow1,
        aTransformRow2);
    mat3x2 mvp = mult3x2(uMvp, transform);
    
    if (aShapeIndex >= 0) {
        ShapeEntry shapeEntry = shapeEntries[aShapeIndex];
        vec2 localPos = mix(shapeEntry.min, shapeEntry.max, corner);
        
        mat2 linear = mat2(mvp[0], mvp[1]);
        vec2 clipNormal = normalize(linear * normal);

        vec2 pixelSize = 2.0 / uViewportSize;
        vec2 dilation = clipNormal * pixelSize * 0.5;

        vec2 worldPos = vec2(mvp * vec3(localPos, 1.0));
        worldPos += dilation;

        mat2 j = jacobian(linear);
        vTexCoord = localPos + j * dilation;
        gl_Position = vec4(worldPos, 0.0, 1.0);
    } else {
        vec2 worldPos = vec2(mvp * vec3(corner, 1.0));
        gl_Position = vec4(worldPos, 0.0, 1.0);
    }
}