#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uLowMipSampler;
uniform sampler2D uHighMipSampler;

in highp vec2 vUv;

uniform highp float uScatter;

void main() {
    // Sample colors
    vec3 lowMipColor = texture(uLowMipSampler, vUv).rgb;
    vec3 highMipColor = texture(uHighMipSampler, vUv).rgb;

    // Mix colors based on scatter
    vec3 color = mix(highMipColor, lowMipColor, uScatter);
    
    // Store result
    oFragColor = vec4(color, 1.0);
}