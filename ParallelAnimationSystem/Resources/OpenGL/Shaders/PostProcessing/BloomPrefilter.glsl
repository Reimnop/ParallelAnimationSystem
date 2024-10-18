#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

const float EPSILON = 0.0001;

layout(rgba16f, binding = 0) uniform image2D uImageInput;
layout(rgba16f, binding = 1) uniform image2D uImageOutput;

uniform ivec2 uSize;
uniform float uThreshold;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    // Load color
    vec4 color = imageLoad(uImageInput, coords);
    
    // Apply threshold (code "borrowed" from Unity URP)
    float knee = uThreshold * 0.5;
    float brightness = max(color.r, max(color.g, color.b));
    float softness = clamp(brightness - uThreshold + knee, 0.0, knee * 2.0);
    softness = softness * softness / (4.0 * knee + EPSILON);
    float multiplier = max(brightness - uThreshold, softness) / max(brightness, EPSILON);
    color.rgb *= multiplier;
    
    // Store result
    imageStore(uImageOutput, coords, color);
}