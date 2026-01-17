#version 300 es

precision highp float;

const highp vec2 VERTICES[] = vec2[](
    vec2(0.0, 1.0),
    vec2(1.0, 1.0),
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(0.0, 0.0),
    vec2(1.0, 1.0)
);

layout(location = 0) in highp vec2 aMin;
layout(location = 1) in highp vec2 aMax;
layout(location = 2) in highp vec2 aMinUv;
layout(location = 3) in highp vec2 aMaxUv;
layout(location = 4) in highp vec4 aColor;
layout(location = 5) in highp int aBoldItalic;
layout(location = 6) in highp int aFontIndex;

out highp vec2 vUvNormalized;
out highp vec2 vUv;
out highp vec4 vGlyphColor;
flat out highp int vBoldItalic;
flat out highp int vFontIndex;

uniform highp mat3x2 uMvp;
uniform highp float uZ;
uniform highp vec4 uBaseColor;

vec4 getGlyphColor() {
    float r = isnan(aColor.r) ? uBaseColor.r : aColor.r;
    float g = isnan(aColor.g) ? uBaseColor.g : aColor.g;
    float b = isnan(aColor.b) ? uBaseColor.b : aColor.b;
    float a = isnan(aColor.a) ? uBaseColor.a : min(uBaseColor.a, aColor.a);
    return vec4(r, g, b, a);
}

void main() {
    vec2 vertPos = VERTICES[gl_VertexID];
    
    vUvNormalized = vertPos;
    vUv = mix(aMinUv, aMaxUv, vertPos);
    vGlyphColor = getGlyphColor();
    vBoldItalic = aBoldItalic;
    vFontIndex = aFontIndex;
    
    bool italic = (aBoldItalic & 2) != 0;
    
    vec2 pos = mix(aMin, aMax, vertPos);
    pos.x += italic ? (pos.y - aMin.y) * 0.2 : 0.0;

    gl_Position = vec4(vec2(uMvp * vec3(pos, 1.0)), uZ, 1.0);
}