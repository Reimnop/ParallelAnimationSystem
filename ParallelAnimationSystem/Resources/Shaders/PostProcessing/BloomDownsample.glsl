#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(binding = 0) uniform sampler2D uSamplerInput;
layout(rgba16f, binding = 1) uniform image2D uImageOutput;

uniform ivec2 uOutputSize;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    // Skip if out of bounds
    if (coords.x >= uOutputSize.x || coords.y >= uOutputSize.y)
        return;
    
    // Calculate UV and pixel size
    vec2 uv = (vec2(coords) + 0.5) / vec2(uOutputSize);
    vec2 pxSize = 1.0 / vec2(uOutputSize);
    
    // Apply bloom downsample
    vec4 a = texture(uSamplerInput, uv + vec2(-pxSize.x, -pxSize.y) * 0.5);
    vec4 b = texture(uSamplerInput, uv + vec2( pxSize.x, -pxSize.y) * 0.5);
    vec4 c = texture(uSamplerInput, uv + vec2(-pxSize.x,  pxSize.y) * 0.5);
    vec4 d = texture(uSamplerInput, uv + vec2( pxSize.x,  pxSize.y) * 0.5);
    
    // Combine them
    vec4 color = (a + b + c + d) * 0.25;
    
    // Store result
    imageStore(uImageOutput, coords, color);
}
