#version 460 core

layout(location = 0) in vec2 aPos;

out vec2 vUv;
out vec2 vUvNormalized;
out vec4 vColor1;
out vec4 vColor2;
out vec2 vTexCoord;
out flat int vRenderMode;
out flat int vRenderType;
out flat int vShapeIndex;

struct MultiDrawItem {
    mat3x2 mvp;
    vec4 color1;
    vec4 color2;
    float z;
    int renderMode;
    int renderType;
    int glyphOffset;
    float gradientRotation;
    float gradientScale;
};

struct RenderGlyph {
    vec4 color;
    mat3x2 transform;
    int shapeEntryIndex;
};

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

layout(std430, binding = 0) buffer MultiDrawBuffer {
    MultiDrawItem multiDrawItems[];
};

layout(std430, binding = 1) buffer GlyphBuffer {
    RenderGlyph glyphs[];
};

layout(std430, binding = 5) readonly buffer ShapeEntryBuffer {
    ShapeEntry shapeEntries[];
};

uniform vec2 uViewportSize;

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
    MultiDrawItem item = multiDrawItems[gl_DrawID];
    vRenderType = item.renderType;
    
    if (item.renderType == 1) {
        RenderGlyph glyph = glyphs[item.glyphOffset + gl_InstanceID];

        vUvNormalized = aPos;
        vColor1 = item.color1 * glyph.color;
        vShapeIndex = glyph.shapeEntryIndex;
        
        mat3x2 finalMvp = mult3x2(item.mvp, glyph.transform);

        vec2 corner = aPos;                             // unit quad corner
        vec2 normal = (aPos - vec2(0.5)) * 2.0;         // outward diagonal (-1,-1)..(1,1)

        if (glyph.shapeEntryIndex >= 0) {
            // Real glyph: place the unit quad over the shape entry's local em-bounds.
            ShapeEntry shapeEntry = shapeEntries[glyph.shapeEntryIndex];
            vec2 localPos = mix(shapeEntry.min, shapeEntry.max, corner);

            // Half-pixel dilation in clip space, with Jacobian-corrected tex coord adjustment, so
            // the curve coverage in the fragment shader anti-aliases cleanly at the silhouette.
            mat2 linear = mat2(finalMvp[0], finalMvp[1]);
            vec2 clipNormal = normalize(linear * normal);
            vec2 pixelSize = 2.0 / uViewportSize;
            vec2 dilation = clipNormal * pixelSize * 0.5;

            vec2 clipPos = vec2(finalMvp * vec3(localPos, 1.0)) + dilation;

            mat2 j = jacobian(linear);
            vTexCoord = localPos + j * dilation;

            gl_Position = vec4(clipPos, item.z, 1.0);
        } else {
            // Mark rectangle: the glyph's transform alone maps the unit quad to world.
            vec2 clipPos = vec2(finalMvp * vec3(corner, 1.0));
            vTexCoord = corner;
            gl_Position = vec4(clipPos, item.z, 1.0);
        }
    } else {
        // Mesh path
        float c = cos(item.gradientRotation) * item.gradientScale;
        float s = sin(item.gradientRotation) * item.gradientScale;
        mat2 uvTransform = mat2(c, -s, s, c);
        
        vUv = uvTransform * aPos + vec2(0.5);
        vUvNormalized = vUv;

        vColor1 = item.color1;
        vColor2 = item.color2;
        vRenderMode = item.renderMode;

        gl_Position = vec4(vec2(item.mvp * vec3(aPos, 1.0)), item.z, 1.0);
    }
}