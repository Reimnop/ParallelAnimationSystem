#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;
uniform float uSampleScale;

vec3 sampleTent(sampler2D mip, vec2 uv, vec2 radius) {
    radius *= 2.0;
    
    // A B C
    // D E F
    // G H I
    vec3 a = texture(mip, uv + vec2(-1.0, -1.0) * radius).rgb;
    vec3 b = texture(mip, uv + vec2( 0.0, -1.0) * radius).rgb;
    vec3 c = texture(mip, uv + vec2( 1.0, -1.0) * radius).rgb;
    vec3 d = texture(mip, uv + vec2(-1.0,  0.0) * radius).rgb;
    vec3 e = texture(mip, uv + vec2( 0.0,  0.0) * radius).rgb;
    vec3 f = texture(mip, uv + vec2( 1.0,  0.0) * radius).rgb;
    vec3 g = texture(mip, uv + vec2(-1.0,  1.0) * radius).rgb;
    vec3 h = texture(mip, uv + vec2( 0.0,  1.0) * radius).rgb;
    vec3 i = texture(mip, uv + vec2( 1.0,  1.0) * radius).rgb;
    
    vec3 color =
        (a + c + g + i) * 1.0 +
        (b + d + f + h) * 2.0 +
        e * 4.0;
    color *= 1.0 / 16.0;
    
    return color;
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;

    // Calculate UV and pixel size
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    vec2 pxSize = 1.0 / vec2(size);
    
    // Sample color
    vec3 srcColor = sampleTent(uSourceSampler, uv, pxSize * uSampleScale);
    
    // Additive blend with current mip level
    vec3 prevColor = imageLoad(uOutputImage, coords).rgb;
    vec3 color = prevColor.rgb + srcColor;
    
    // Store result
    imageStore(uOutputImage, coords, vec4(color, 1.0));
}