#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

in highp vec2 vUv;

uniform highp int uRenderMode;
uniform highp vec4 uColor1;
uniform highp vec4 uColor2;

vec4 getColor(vec4 color1, vec4 color2, int mode, vec2 uv) {
    // mode 0: color1
    // mode 1: gradient 1 to 2 on x
    // mode 2: gradient 2 to 1 on x
    // mode 3: circular gradient inwards
    // mode 4: circular gradient outwards
    if (mode == 0) {
        return color1;
    } else if (mode == 1) {
        return mix(color1, color2, uv.x);
    } else if (mode == 2) {
        return mix(color2, color1, uv.x);
    } else if (mode == 3) {
        float dist = min(distance(uv, vec2(0.5)) * 2.0, 1.0);
        return mix(color2, color1, dist);
    } else if (mode == 4) {
        float dist = min(distance(uv, vec2(0.5)) * 2.0, 1.0);
        return mix(color1, color2, dist);
    }
    return color1;
}

void main() {
    oFragColor = getColor(uColor1, uColor2, uRenderMode, vUv);
}