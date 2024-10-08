#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

in highp vec2 vUvNormalized;
in highp vec2 vUv;
in highp vec4 vGlyphColor;
flat in highp int vBoldItalic;
flat in highp int vFontIndex;

uniform sampler2D uFontAtlases[12];

float screenPxRange() {
    vec2 unitRange = vec2(2.0) / vec2(32.0); // TODO: Don't hardcode this: pxRange / fontSize
    vec2 screenTexSize = vec2(1.0) / fwidth(vUvNormalized);
    return max(dot(unitRange, screenTexSize), 0.0);
}

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

vec4 getFontAtlas(int index, vec2 uv) {
    switch (index) {
        case 0: return texture(uFontAtlases[0], uv);
        case 1: return texture(uFontAtlases[1], uv);
        case 2: return texture(uFontAtlases[2], uv);
        case 3: return texture(uFontAtlases[3], uv);
        case 4: return texture(uFontAtlases[4], uv);
        case 5: return texture(uFontAtlases[5], uv);
        case 6: return texture(uFontAtlases[6], uv);
        case 7: return texture(uFontAtlases[7], uv);
        case 8: return texture(uFontAtlases[8], uv);
        case 9: return texture(uFontAtlases[9], uv);
        case 10: return texture(uFontAtlases[10], uv);
        case 11: return texture(uFontAtlases[11], uv);
        default: return vec4(1.0);
    }
}

void main() {
    // Output solid color if font index is < 0
    if (vFontIndex < 0) {
        oFragColor = vGlyphColor;
    } else {
        vec3 msdf = getFontAtlas(vFontIndex, vUv).rgb;
        float distance = median(msdf.r, msdf.g, msdf.b);
        float pxDistance = screenPxRange() * (distance - ((vBoldItalic & 1) != 0 ? 0.2 : 0.5));
        float alpha = clamp(pxDistance + 0.5, 0.0, 1.0);
        oFragColor = vec4(vGlyphColor.rgb, vGlyphColor.a * alpha);
    }
}