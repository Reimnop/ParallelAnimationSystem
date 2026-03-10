#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;
layout(rgba16f, binding = 0) uniform image2D uImageOutput;

uniform sampler2D uTexture;
uniform sampler2D uNoiseTexture;

uniform float uIntensity;
uniform float uColorIntensity;

vec4 glitch(sampler2D tex, sampler2D noiseTex, vec2 uv, float intensity, float colorIntensity) {
    vec3 glitch = texture(noiseTex, uv).xyz;
    float thresh = 1.001 - intensity * 1.001;
    float w_d = step(thresh * colorIntensity, pow(abs(glitch.z), 2.5));
    float w_c = step(thresh, pow(abs(glitch.z), 3.5));
    vec2 uv2 = fract(uv + glitch.xy * w_d);
    vec4 source = texture(tex, uv2);
    vec3 color = source.rgb;
    vec3 neg = clamp(color.grb + (1.0 - dot(color, vec3(1.0))) * 0.1, 0.0, 1.0);
    color = mix(color, neg, w_c);
    return vec4(color, source.a);
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uImageOutput);
    
    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    // Get texture coordinates
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    
    vec4 color = glitch(uTexture, uNoiseTexture, uv, uIntensity, uColorIntensity);
    
    // Store result
    imageStore(uImageOutput, coords, color);
}