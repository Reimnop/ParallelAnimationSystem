#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;
uniform sampler2D uNoiseSampler;

uniform bool uColored;
uniform highp float uLuminanceContribution;
uniform highp float uIntensity;
uniform highp vec2 uScale;
uniform highp vec2 uOffset;

in highp vec2 vUv;

vec3 gammaToLinear(vec3 color) {
    return color * color;
}

vec3 linearToGamma(vec3 color) {
    return sqrt(color);
}

float luminance(vec3 color) {
    return dot(color, vec3(0.2126729, 0.7151522, 0.0721750));
}

void main() {
    vec3 color = gammaToLinear(texture(uSourceSampler, vUv).rgb);
    
    vec4 noiseRaw = texture(uNoiseSampler, vUv * uScale + uOffset);
    vec3 grain = uColored ? noiseRaw.rgb : vec3(noiseRaw.r);
    
    float lum = 1.0 - sqrt(luminance(clamp(color, 0.0, 1.0)));
    lum = mix(1.0, lum, uLuminanceContribution);
    
    color += color * grain * lum * uIntensity;
    
    color = linearToGamma(color);

    oFragColor = vec4(color, 1.0);
}