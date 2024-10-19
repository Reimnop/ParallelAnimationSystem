#version 300 es

precision highp float;

const float EPSILON = 0.0001;

layout(location = 0) out highp vec4 oFragColor;

in highp vec2 vUv;

uniform sampler2D uTexture;
uniform highp float uThreshold;

void main() {
    // Load color
    vec4 color = texture(uTexture, vUv);

    // Apply threshold (code "borrowed" from Unity URP)
    float knee = uThreshold * 0.5;
    float brightness = max(color.r, max(color.g, color.b));
    float softness = clamp(brightness - uThreshold + knee, 0.0, knee * 2.0);
    softness = softness * softness / (4.0 * knee + EPSILON);
    float multiplier = max(brightness - uThreshold, softness) / max(brightness, EPSILON);
    color.rgb *= multiplier;
    
    // Store result
    oFragColor = color;
}