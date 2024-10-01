#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

in highp vec2 vUv;

uniform sampler2D uTexture;

void main() {
    // Load color
    vec4 color = texture(uTexture, vUv);

    // Calculate luminance
    float luminance = dot(color.rgb, vec3(0.299, 0.587, 0.114));

    // Apply threshold (smooth)
    color.rgb *= smoothstep(0.5, 1.0, luminance);
    color.a = 1.0;
    
    // Store result
    oFragColor = color;
}