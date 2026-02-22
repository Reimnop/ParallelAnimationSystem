#version 460 core

const int MAX_SOURCES = 10;

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform vec2 uSourceScales[MAX_SOURCES];
uniform vec2 uSourceOffsets[MAX_SOURCES];
uniform sampler2D uSourceSamplers[MAX_SOURCES];
uniform int uSourceCount;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    // Calculate uv
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    
    // Combine all source textures
    vec4 color = vec4(0.0);
    for (int i = 0; i < uSourceCount; i++) {
        vec2 srcUv = uv;
        srcUv -= uSourceOffsets[i];
        srcUv /= uSourceScales[i];
        vec4 srcColor = texture(uSourceSamplers[i], srcUv);

        // src alpha, one minus src alpha, one, one minus src alpha
        color.rgb = color.rgb * (1.0 - srcColor.a) + srcColor.rgb * srcColor.a;
        color.a = color.a * (1.0 - color.a) + srcColor.a;
    }
    
    // Store result
    imageStore(uOutputImage, coords, color);
}