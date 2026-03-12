#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

const float EPSILON = 0.0001;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;

uniform float uThreshold;
uniform float uKnee;

// "Borrowed" from Unity URP
vec3 applyThreshold(vec3 color, float threshold, float knee) {
    float brightness = max(max(color.r, color.g), color.b);
    float softness = clamp(brightness - threshold + knee, 0.0, 2.0 * knee);
    softness = softness * softness / (4.0 * knee + EPSILON);
    float multiplier = max(brightness - threshold, softness) / max(brightness, EPSILON);
    color *= multiplier;
    return color;
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);
    
    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    
    vec3 color = texture(uSourceSampler, uv).rgb;
    color = applyThreshold(color, uThreshold, uKnee);
    
    imageStore(uOutputImage, coords, vec4(color, 1.0));
}