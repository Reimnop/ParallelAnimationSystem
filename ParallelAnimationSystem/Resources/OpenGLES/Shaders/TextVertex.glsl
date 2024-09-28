#version 300 es

precision highp float;

layout(location = 0) in highp vec2 aPos;

out highp vec2 vUvNormalized;
out highp vec2 vUv;

uniform highp mat3 uMvp;
uniform highp float uZ;
uniform highp vec4 uMinMax;
uniform highp vec4 uUv;
uniform highp int uBoldItalic;

void main() {
    vUvNormalized = aPos;
    vUv = mix(uUv.xy, uUv.zw, aPos);
    
    bool italic = (uBoldItalic & 2) != 0;
    
    vec2 pos = mix(uMinMax.xy, uMinMax.zw, aPos);
    pos.x += italic ? (pos.y - uMinMax.y) * 0.2 : 0.0;

    gl_Position = vec4(vec2(uMvp * vec3(pos, 1.0)), uZ, 1.0);
}