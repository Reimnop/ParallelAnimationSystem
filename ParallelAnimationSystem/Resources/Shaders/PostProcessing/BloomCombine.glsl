#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba8, binding = 0) uniform image2D uImageInput1;
layout(rgba16f, binding = 1) uniform image2D uImageInput2;
layout(rgba8, binding = 2) uniform image2D uImageOutput;

uniform ivec2 uSize;
uniform float uIntensity;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    // Load colors
    vec4 color1 = imageLoad(uImageInput1, coords);
    vec4 color2 = imageLoad(uImageInput2, coords);
    
    // Combine them
    vec4 color = color1 + color2 * uIntensity;
    
    // Store result
    imageStore(uImageOutput, coords, color);
}