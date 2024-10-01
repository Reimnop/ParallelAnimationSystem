#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uLowMip;
uniform sampler2D uHighMip;

uniform highp ivec2 uSize;
uniform highp float uDiffusion;

in highp vec2 vUv;

void main() {
    // Fetch current color
    vec4 lowMipColor = texture(uLowMip, vUv);
    
    // Fetch original color
    vec4 highMipColor = texture(uHighMip, vUv);
    
    // Mix
    vec4 color = mix(highMipColor, lowMipColor, uDiffusion);
    
    // Store result
    oFragColor = color;
}