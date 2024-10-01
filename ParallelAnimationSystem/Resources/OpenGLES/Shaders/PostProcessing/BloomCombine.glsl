#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uTexture1;
uniform sampler2D uTexture2;

uniform highp float uIntensity;

in highp vec2 vUv;

void main() {
    // Fetch colors
    vec4 color1 = texture(uTexture1, vUv);
    vec4 color2 = texture(uTexture2, vUv);
    
    // Mix
    vec4 color = color1 + color2 * uIntensity;
    color.a = 1.0;
    
    // Store result
    oFragColor = color;
}