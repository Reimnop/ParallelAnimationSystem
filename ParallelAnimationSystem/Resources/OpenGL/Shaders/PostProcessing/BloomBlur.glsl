#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

const float[] kernel = float[]( 
    0.01621622, 0.05405405, 0.12162162, 0.19459459, 
    0.22702703,
    0.19459459, 0.12162162, 0.05405405, 0.01621622
);

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uTexture;

uniform ivec2 uSize;
uniform bool uVertical;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(uSize);
    vec2 pxSize = 1.0 / vec2(uSize);
    
    // Add all the colors
    vec4 color = vec4(0.0);
    for (int i = 0; i < 9; i++) {
        vec2 currentUv = uVertical 
            ? uv + vec2( 0.0,     -4.0 + i) * pxSize 
            : uv + vec2(-4.0 + i,  0.0    ) * pxSize;
        vec4 currentColor = texture(uTexture, currentUv);
        color += currentColor * kernel[i];
    }
    
    // Store the result
    imageStore(uOutputImage, coords, color);
}