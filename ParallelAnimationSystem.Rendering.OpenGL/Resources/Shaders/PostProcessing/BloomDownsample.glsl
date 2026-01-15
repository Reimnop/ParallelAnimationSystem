#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uTexture;
uniform highp ivec2 uSize;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uSize.x || coords.y >= uSize.y)
        return;
    
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(uSize);
    vec2 pxSize = 1.0 / vec2(uSize);
    
    // A B C
    //  J K
    // D E F
    //  L M
    // G H I
    vec3 a = texture(uTexture, uv + vec2(-1.0, -1.0) * pxSize).rgb;
    vec3 b = texture(uTexture, uv + vec2( 0.0, -1.0) * pxSize).rgb;
    vec3 c = texture(uTexture, uv + vec2( 1.0, -1.0) * pxSize).rgb;
    vec3 d = texture(uTexture, uv + vec2(-1.0,  0.0) * pxSize).rgb;
    vec3 e = texture(uTexture, uv + vec2( 0.0,  0.0) * pxSize).rgb;
    vec3 f = texture(uTexture, uv + vec2( 1.0,  0.0) * pxSize).rgb;
    vec3 g = texture(uTexture, uv + vec2(-1.0,  1.0) * pxSize).rgb;
    vec3 h = texture(uTexture, uv + vec2( 0.0,  1.0) * pxSize).rgb;
    vec3 i = texture(uTexture, uv + vec2( 1.0,  1.0) * pxSize).rgb;
    vec3 j = texture(uTexture, uv + vec2(-0.5, -0.5) * pxSize).rgb;
    vec3 k = texture(uTexture, uv + vec2( 0.5, -0.5) * pxSize).rgb;
    vec3 l = texture(uTexture, uv + vec2(-0.5,  0.5) * pxSize).rgb;
    vec3 m = texture(uTexture, uv + vec2( 0.5,  0.5) * pxSize).rgb;
    
    vec3 color =
        e * 0.125 +
        (a + c + g + i) * 0.03125 +
        (b + d + f + h) * 0.0625 +
        (j + k + l + m) * 0.125;
    
    // Store the result
    imageStore(uOutputImage, coords, vec4(color, 1.0));
}