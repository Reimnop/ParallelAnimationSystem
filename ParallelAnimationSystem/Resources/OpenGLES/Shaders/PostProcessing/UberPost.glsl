#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

const float PI = 3.14159265359;

uniform sampler2D uTexture;

uniform ivec2 uSize;
uniform highp float uHueShiftAngle;
uniform highp float uLensDistortionIntensity;
uniform highp vec2 uLensDistortionCenter;

in highp vec2 vUv;

// "Borrowed" from https://gist.github.com/mairod/a75e7b44f68110e1576d77419d608786
vec3 hueShift(vec3 color, float hueAdjust) {
    const vec3 kRGBToYPrime = vec3(0.299, 0.587, 0.114);
    const vec3 kRGBToI      = vec3(0.596, -0.275, -0.321);
    const vec3 kRGBToQ      = vec3(0.212, -0.523, 0.311);

    const vec3 kYIQToR = vec3(1.0, 0.956, 0.621);
    const vec3 kYIQToG = vec3(1.0, -0.272, -0.647);
    const vec3 kYIQToB = vec3(1.0, -1.107, 1.704);

    float YPrime  = dot(color, kRGBToYPrime);
    float I       = dot(color, kRGBToI);
    float Q       = dot(color, kRGBToQ);
    float hue     = atan(Q, I);
    float chroma  = sqrt(I * I + Q * Q);

    hue += hueAdjust;

    Q = chroma * sin(hue);
    I = chroma * cos(hue);

    vec3 yIQ = vec3(YPrime, I, Q);

    return vec3(dot(yIQ, kYIQToR), dot(yIQ, kYIQToG), dot(yIQ, kYIQToB));
}

// "Borrowed" from Unity's PostProcessing package
vec2 distortLens(vec2 uv, float intensity, vec2 center) {
    if (intensity == 0.0)
        return uv;

    float amount = 1.6 * max(abs(intensity), 1.0);
    float theta = min(160.0, amount) * PI / 180.0;
    float sigma = 2.0 * tan(theta * 0.5);

    vec2 ruv = uv - vec2(0.5) - center;
    float ru = length(ruv);

    if (intensity > 0.0) {
        float wu = ru * theta;
        ru = tan(wu) * (1.0 / (ru * sigma));
        uv = uv + ruv * (ru - 1.0);
    } else {
        ru = (1.0 / ru) * (1.0 / theta) * atan(ru * sigma);
        uv = uv + ruv * (ru - 1.0);
    }

    return uv;
}

void main() {
    // Apply lens distortion
    vec2 uv = distortLens(vUv, uLensDistortionIntensity, uLensDistortionCenter);
    
    // Load color
    vec4 color = texture(uTexture, uv);
    
    // Hue shift
    color.rgb = hueShift(color.rgb, uHueShiftAngle);
    
    // Store result
    oFragColor = color;
}