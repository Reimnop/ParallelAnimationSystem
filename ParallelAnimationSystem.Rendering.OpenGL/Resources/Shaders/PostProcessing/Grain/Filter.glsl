#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;
layout(r16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;
uniform vec2 uParams; // x = b, y = c

float applyFilter(vec2 uv, vec2 texelSize, float b, float c) {
    return (1.0 / (4.0 + b * 4.0 + abs(c))) * (
        texture(uSourceSampler, uv + vec2(-1.0, -1.0) * texelSize).r +
        texture(uSourceSampler, uv + vec2( 0.0, -1.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2( 1.0, -1.0) * texelSize).r +
        texture(uSourceSampler, uv + vec2(-1.0,  0.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2( 0.0,  0.0) * texelSize).r * c +
        texture(uSourceSampler, uv + vec2( 1.0,  0.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2(-1.0,  1.0) * texelSize).r +
        texture(uSourceSampler, uv + vec2( 0.0,  1.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2( 1.0,  1.0) * texelSize).r
    );
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;

    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    vec2 texelSize = 1.0 / vec2(textureSize(uSourceSampler, 0));
    float n = applyFilter(uv, texelSize, uParams.x, uParams.y);

    imageStore(uOutputImage, coords, vec4(vec3(n), 1.0));
}