#version 460 core

layout(location = 0) out vec4 fragColor;

in vec2 vUv;

uniform int uRenderMode;
uniform vec4 uColor1;
uniform vec4 uColor2;

vec4 getColor(vec4 color1, vec4 color2, int mode, vec2 uv) {
    // mode 0: color1
    // mode 1: gradient 1 to 2 on x
    // mode 2: gradient 2 to 1 on x
    // mode 3: circular gradient inwards
    // mode 4: circular gradient outwards
    
    // if color2 is the same as color1,
    // make it fade out
    if (color2 == color1)
        color2.a = 0.0;
    
    if (mode == 0) {
        return color1;
    } else if (mode == 1) {
        return mix(color1, color2, uv.x);
    } else if (mode == 2) {
        return mix(color2, color1, uv.x);
    } else if (mode == 3) {
        float dist = distance(uv, vec2(0.5));
        return mix(color2, color1, dist);
    } else if (mode == 4) {
        float dist = distance(uv, vec2(0.5));
        return mix(color1, color2, dist);
    }
    
    return color1;
}

void main() {
    fragColor = getColor(uColor1, uColor2, uRenderMode, vUv);
}