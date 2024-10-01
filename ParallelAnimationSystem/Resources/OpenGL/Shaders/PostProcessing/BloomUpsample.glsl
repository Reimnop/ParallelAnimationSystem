#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(binding = 0) uniform sampler2D uSamplerInput;
layout(rgba16f, binding = 1) uniform image2D uImageOutput;

uniform ivec2 uOutputSize;
uniform float uDiffusion;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uOutputSize.x || coords.y >= uOutputSize.y)
        return;

    // Calculate UV and pixel size
    vec2 uv = (vec2(coords) + 0.5) / vec2(uOutputSize);
    vec2 pxSize = 1.0 / vec2(uOutputSize);
    
    // Fetch current color
    vec4 lowMipColor = texture(uSamplerInput, uv);
    
    // Fetch original color
    vec4 highMipColor = imageLoad(uImageOutput, coords);
    
    // Mix
    vec4 color = mix(highMipColor, lowMipColor, uDiffusion);
    
    // Store result
    imageStore(uImageOutput, coords, color);
}