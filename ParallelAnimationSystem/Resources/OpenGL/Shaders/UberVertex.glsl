#version 460 core

layout(location = 0) in vec2 aPos;

out vec2 vUv;
out vec2 vUvNormalized;
out vec4 vColor1;
out vec4 vColor2;
out flat int vRenderMode;
out flat int vRenderType;
out flat int vBoldItalic;
out flat int vFontIndex;

struct MultiDrawItem {
    mat3 mvp;
    vec4 color1;
    vec4 color2;
    float z;
    int renderMode;
    int renderType;
    int glyphOffset;
};

struct RenderGlyph {
    vec4 minMax;
    vec4 uv;
    vec4 color; // NaN = inherit
    int boldItalic;
    int fontIndex;
};

layout(std430, binding = 0) buffer MultiDrawBuffer {
    MultiDrawItem multiDrawItems[];
};

layout(std430, binding = 1) buffer GlyphBuffer {
    RenderGlyph glyphs[];
};

void main() {
    // Get the data for this draw call
    MultiDrawItem item = multiDrawItems[gl_DrawID];
    vRenderType = item.renderType;
    
    if (item.renderType == 1) {
        RenderGlyph glyph = glyphs[item.glyphOffset + gl_InstanceID];

        vUv = mix(glyph.uv.xy, glyph.uv.zw, aPos);
        vUvNormalized = aPos;
        vColor1 = item.color1;
        vColor2 = glyph.color;
        vBoldItalic = glyph.boldItalic;
        vFontIndex = glyph.fontIndex;

        bool italic = (glyph.boldItalic & 2) != 0;

        vec2 min = glyph.minMax.xy;
        vec2 max = glyph.minMax.zw;
        vec2 pos = mix(min, max, aPos);
        pos.x += italic ? (pos.y - min.y) * 0.2 : 0.0;

        gl_Position = vec4(vec2(item.mvp * vec3(pos, 1.0)), item.z, 1.0);
    } else {
        // Since aPos is always in the range [-0.5, 0.5],
        // we can use it to directly calculate UVs,
        // without wasting space in the vertex buffer
        vUv = aPos + vec2(0.5);
        vUvNormalized = vUv;

        // Pass the data to the fragment shader
        vColor1 = item.color1;
        vColor2 = item.color2;
        vRenderMode = item.renderMode;

        // Calculate the vertex position
        gl_Position = vec4(vec2(item.mvp * vec3(aPos, 1.0)), item.z, 1.0);
    }
}