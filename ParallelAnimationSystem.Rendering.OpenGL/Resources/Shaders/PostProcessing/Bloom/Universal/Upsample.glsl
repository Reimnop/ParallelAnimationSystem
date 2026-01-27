#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;
uniform float uScatter;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;

    // Calculate UV and pixel size
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);

    // Sample colors
    vec3 highMipColor = imageLoad(uOutputImage, coords).rgb;
    vec3 lowMipColor = texture(uSourceSampler, uv).rgb;
    
    // Mix colors based on scatter
    vec3 color = mix(highMipColor, lowMipColor, uScatter);
    
    // Store result
    imageStore(uOutputImage, coords, vec4(color, 1.0));
}