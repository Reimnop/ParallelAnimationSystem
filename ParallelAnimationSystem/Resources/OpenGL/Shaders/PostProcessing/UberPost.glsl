#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

const float PI = 3.14159265359;

layout(rgba16f, binding = 0) uniform image2D uImageOutput;
uniform sampler2D uTexture;

uniform ivec2 uSize;
uniform float uHueShiftAngle;
uniform float uLensDistortionIntensity;
uniform vec2 uLensDistortionCenter;
uniform float uChromaticAberrationIntensity;
uniform vec2 uVignetteCenter;
uniform float uVignetteIntensity;
uniform float uVignetteRoundness;
uniform float uVignetteSmoothness;
uniform vec3 uVignetteColor;

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
vec2 distortLens(vec2 uv) {
    float intensity = uLensDistortionIntensity;
    vec2 center = uLensDistortionCenter;
    
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

vec3 vignette(vec3 color, vec2 uv, vec2 center, float intensity, float roundness, float smoothness, vec3 vignetteColor) {
    vec2 dist = abs(uv - center) * intensity;
    dist.x *= roundness;
    float vFactor = pow(clamp(1.0 - dot(dist, dist), 0.0, 1.0), smoothness);
    return color * mix(vignetteColor, vec3(1.0), vFactor);
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    // Get texture coordinates
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(uSize);
    
    // Apply lens distortion
    vec2 uvDistorted = distortLens(uv);
    
    // Apply chromatic aberration
    vec3 color;
    if (uChromaticAberrationIntensity == 0.0) {
        color = texture(uTexture, uvDistorted).rgb;
    } else {
        vec2 coords = uv * 2.0 - 1.0;
        vec2 end = uv - coords * dot(coords, coords) * uChromaticAberrationIntensity;
        vec2 delta = (end - uv) / 3.0;

        float r = texture(uTexture, uvDistorted).r;
        float g = texture(uTexture, distortLens(uv + delta)).g;
        float b = texture(uTexture, distortLens(uv + delta * 2.0)).b;

        color = vec3(r, g, b);
    }
    
    // Apply vignette
    if (uVignetteIntensity != 0.0) {
        color = vignette(color, uvDistorted, uVignetteCenter, uVignetteIntensity, uVignetteRoundness, uVignetteSmoothness, uVignetteColor);
    }
    
    // Hue shift
    if (uHueShiftAngle != 0.0) {
        color = hueShift(color, uHueShiftAngle);
    }
    
    // Store result
    imageStore(uImageOutput, coords, vec4(color, 1.0));
}