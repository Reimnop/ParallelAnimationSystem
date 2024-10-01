#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

const float[] kernel = float[]( 
    0.01621622, 0.05405405, 0.12162162, 0.19459459, 
    0.22702703,
    0.19459459, 0.12162162, 0.05405405, 0.01621622
);

layout(rgba16f, binding = 0) uniform image2D uInputImage;
layout(rgba16f, binding = 1) uniform image2D uOutputImage;

uniform ivec2 uSize;
uniform bool uVertical;

ivec2 clampCoords(ivec2 coords) {
    return ivec2(clamp(coords.x, 0, uSize.x - 1), clamp(coords.y, 0, uSize.y - 1));
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    // Add all the colors
    vec4 color = vec4(0.0);
    for (int i = 0; i < 9; i++) {
        ivec2 currentCoords = uVertical 
            ? clampCoords(coords + ivec2(0, -4 + i)) 
            : clampCoords(coords + ivec2(-4 + i, 0));
        vec4 currentColor = imageLoad(uInputImage, currentCoords);
        color += currentColor * kernel[i];
    }
    
    // Store the result
    imageStore(uOutputImage, coords, color);
}