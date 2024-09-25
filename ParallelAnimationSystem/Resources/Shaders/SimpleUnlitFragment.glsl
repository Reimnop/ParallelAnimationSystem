#version 460 core

layout(location = 0) out vec4 oFragColor;

uniform sampler2D uFontAtlases[12];

in vec2 vUv;
in vec2 vUvNormalized;
in vec4 vColor1;
in vec4 vColor2;
in flat int vRenderMode;
in flat int vRenderType;
in flat int vBold;
in flat int vFontIndex;

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

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

float screenPxRange() {
    vec2 unitRange = vec2(2.0) / vec2(32.0); // TODO: Don't hardcode this: pxRange / fontSize
    vec2 screenTexSize = vec2(1.0) / fwidth(vUvNormalized);
    return max(dot(unitRange, screenTexSize), 0.0);
}

vec4 getColor() {
    float r = isnan(vColor2.r) ? vColor1.r : vColor2.r;
    float g = isnan(vColor2.g) ? vColor1.g : vColor2.g;
    float b = isnan(vColor2.b) ? vColor1.b : vColor2.b;
    float a = isnan(vColor2.a) ? vColor1.a : vColor2.a;
    return vec4(r, g, b, a);
}

void main() {
    if (vRenderType == 1) {
        vec3 msdf = texture(uFontAtlases[vFontIndex], vUv).rgb;
        float distance = median(msdf.r, msdf.g, msdf.b);
        float pxDistance = screenPxRange() * (distance - (vBold == 0 ? 0.5 : 0.2));
        float alpha = clamp(pxDistance + 0.5, 0.0, 1.0);
        vec4 color = getColor();
        oFragColor = vec4(color.rgb, color.a * alpha);
    } else {
        oFragColor = getColor(vColor1, vColor2, vRenderMode, vUv);
    }
}