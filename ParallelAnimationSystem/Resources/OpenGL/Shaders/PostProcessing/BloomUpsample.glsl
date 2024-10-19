#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uImageOutput;

uniform sampler2D uLowMipSampler;
uniform sampler2D uHighMipSampler;

uniform ivec2 uOutputSize;
uniform float uDiffusion;

vec3 sampleLowMip(sampler2D mip, vec2 uv, vec2 pxSize) {
    // A B C
    // D E F
    // G H I
    vec3 a = texture(mip, uv + vec2(-1.0, -1.0) * pxSize).rgb;
    vec3 b = texture(mip, uv + vec2( 0.0, -1.0) * pxSize).rgb;
    vec3 c = texture(mip, uv + vec2( 1.0, -1.0) * pxSize).rgb;
    vec3 d = texture(mip, uv + vec2(-1.0,  0.0) * pxSize).rgb;
    vec3 e = texture(mip, uv + vec2( 0.0,  0.0) * pxSize).rgb;
    vec3 f = texture(mip, uv + vec2( 1.0,  0.0) * pxSize).rgb;
    vec3 g = texture(mip, uv + vec2(-1.0,  1.0) * pxSize).rgb;
    vec3 h = texture(mip, uv + vec2( 0.0,  1.0) * pxSize).rgb;
    vec3 i = texture(mip, uv + vec2( 1.0,  1.0) * pxSize).rgb;
    
    vec3 color =
        (a + c + g + i) * 1.0 +
        (b + d + f + h) * 2.0 +
        e * 4.0;
    color *= 0.0625;
    
    return color;
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uOutputSize.x || coords.y >= uOutputSize.y)
        return;

    // Calculate UV and pixel size
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(uOutputSize);
    vec2 pxSize = 1.0 / vec2(uOutputSize);
    
    // Fetch current color
    vec3 lowMipColor = sampleLowMip(uLowMipSampler, uv, pxSize * 0.5);
    
    // Fetch original color
    vec3 highMipColor = texture(uHighMipSampler, uv).rgb;
    
    // Mix
    vec3 color = mix(highMipColor, lowMipColor, uDiffusion);
    
    // Store result
    imageStore(uImageOutput, coords, vec4(color, 1.0));
}