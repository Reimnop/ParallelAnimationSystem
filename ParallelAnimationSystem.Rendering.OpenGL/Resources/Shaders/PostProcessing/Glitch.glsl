#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;
layout(rgba16f, binding = 0) uniform image2D uImageOutput;

uniform sampler2D uTexture;
uniform sampler2D uNoiseTexture;

uniform float uIntensity;
uniform float uColorIntensity;
void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uImageOutput);
    
    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    // Get texture coordinates
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);

    vec3 glitch = texture(uNoiseTexture, uv).rgb;
    float activeAmount = pow(uIntensity, 1.8);
    float thresh = 1.0 - activeAmount;
    float w_c = step(thresh * 0.99, glitch.z);
    float w_d = step(thresh, glitch.z);
    float scrambleScale = mix(0.04, 0.30, uIntensity);
    vec2 offset = (glitch.xy - 0.5) * scrambleScale;
    vec2 uv2 = fract(uv + offset * w_d);
    vec4 source = texture(uTexture, uv2);
    vec3 color = source.rgb;
    vec3 neg = clamp(color.grb + (1.0 - dot(color, vec3(1.0))) * 0.1, 0.0, 1.0);
    color = mix(color, neg, w_c);
    
    // Store result
    imageStore(uImageOutput, coords, vec4(color, 1.0));
}