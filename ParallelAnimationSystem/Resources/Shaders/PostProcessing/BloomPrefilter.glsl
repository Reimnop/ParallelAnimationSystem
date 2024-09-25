#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uImageInput;
layout(rgba16f, binding = 1) uniform image2D uImageOutput;

uniform ivec2 uSize;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    // Load color
    vec4 color = imageLoad(uImageInput, coords);
    
    // Calculate luminance
    float luminance = dot(color.rgb, vec3(0.299, 0.587, 0.114));
    
    // Apply threshold (smooth)
    color.rgb *= smoothstep(0.5, 0.8, luminance);
    color.a = 1.0;
    
    // Store result
    imageStore(uImageOutput, coords, color);
}