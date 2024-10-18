#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uImageOutput;

uniform sampler2D uLowMipSampler;
uniform sampler2D uHighMipSampler;

uniform ivec2 uOutputSize;
uniform float uDiffusion;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uOutputSize.x || coords.y >= uOutputSize.y)
        return;

    // Calculate UV and pixel size
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(uOutputSize);
    vec2 pxSize = 1.0 / vec2(uOutputSize);
    
    // Fetch current color
    vec4 lowMipColor = texture(uLowMipSampler, uv);
    
    // Fetch original color
    vec4 highMipColor = texture(uHighMipSampler, uv);
    
    // Mix
    vec4 color = mix(highMipColor, lowMipColor, uDiffusion);
    
    // Store result
    imageStore(uImageOutput, coords, color);
}