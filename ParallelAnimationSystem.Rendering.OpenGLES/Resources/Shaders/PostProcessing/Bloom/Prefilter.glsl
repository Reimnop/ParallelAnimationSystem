#version 300 es

precision highp float;

const float EPSILON = 0.0001;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;

in highp vec2 vUv;

uniform highp float uThreshold;
uniform highp float uKnee;

// "Borrowed" from Unity URP
vec3 applyThreshold(vec3 color, float threshold, float knee) {
    float brightness = max(max(color.r, color.g), color.b);
    float softness = clamp(brightness - threshold + knee, 0.0, 2.0 * knee);
    softness = softness * softness / (4.0 * knee + EPSILON);
    float multiplier = max(brightness - threshold, softness) / max(brightness, EPSILON);
    color *= multiplier;
    return color;
}

void main() {
    // Load color
    vec3 color = texture(uSourceSampler, vUv).rgb;

    color *= color; // Convert to linear space
    
    // Apply threshold
    color = applyThreshold(color, uThreshold, uKnee);
    
    // Store result
    oFragColor = vec4(color, 1.0);
}