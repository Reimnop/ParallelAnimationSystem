#version 460 core

const vec3 NOISE_PARAMS = vec3(12.9898, 78.233, 43758.5453);

layout(local_size_x = 8, local_size_y = 8) in;

layout(r16f, binding = 0) uniform image2D uOutputImage;

uniform float uPhase;

float noise(vec2 uv, float phase) {
    vec2 n = uv + phase;
    return fract(sin(dot(n, NOISE_PARAMS.xy)) * NOISE_PARAMS.z);
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);
    
    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    float n = noise(uv, uPhase);
    
    imageStore(uOutputImage, coords, vec4(vec3(n), 1.0));
}