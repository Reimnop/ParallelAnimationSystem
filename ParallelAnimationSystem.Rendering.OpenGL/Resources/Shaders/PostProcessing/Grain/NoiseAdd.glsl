#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;

uniform int uChannel;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;

    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    
    vec3 color = imageLoad(uOutputImage, coords).rgb;
    float noise = texture(uSourceSampler, uv).r;
    color[uChannel] = noise;

    imageStore(uOutputImage, coords, vec4(color, 1.0));
}