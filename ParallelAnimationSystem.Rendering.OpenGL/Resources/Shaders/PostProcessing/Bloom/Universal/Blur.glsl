#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;
uniform bool uIsVertical;

// 9-tap guassian blur on downsampled image
vec3 blurH(sampler2D mip, vec2 uv, vec2 pxSize) {
    vec3 c0 = texture(mip, uv + vec2(-4.0, 0.0) * pxSize).rgb;
    vec3 c1 = texture(mip, uv + vec2(-3.0, 0.0) * pxSize).rgb;
    vec3 c2 = texture(mip, uv + vec2(-2.0, 0.0) * pxSize).rgb;
    vec3 c3 = texture(mip, uv + vec2(-1.0, 0.0) * pxSize).rgb;
    vec3 c4 = texture(mip, uv + vec2( 0.0, 0.0) * pxSize).rgb;
    vec3 c5 = texture(mip, uv + vec2( 1.0, 0.0) * pxSize).rgb;
    vec3 c6 = texture(mip, uv + vec2( 2.0, 0.0) * pxSize).rgb;
    vec3 c7 = texture(mip, uv + vec2( 3.0, 0.0) * pxSize).rgb;
    vec3 c8 = texture(mip, uv + vec2( 4.0, 0.0) * pxSize).rgb;
    
    // sum weighted color
    vec3 color = 
        c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
        + c4 * 0.22702703
        + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;
    
    return color;
}

// 5-tap guassian blur on same sized image
// this is equivalent to applying a 9-tap convolution
// thanks to bilinear filtering
vec3 blurV(sampler2D mip, vec2 uv, vec2 pxSize) {
    vec3 c0 = texture(mip, uv + vec2(0.0, -3.23076923) * pxSize).rgb;
    vec3 c1 = texture(mip, uv + vec2(0.0, -1.38461538) * pxSize).rgb;
    vec3 c2 = texture(mip, uv + vec2(0.0,  0.0       ) * pxSize).rgb;
    vec3 c3 = texture(mip, uv + vec2(0.0,  1.38461538) * pxSize).rgb;
    vec3 c4 = texture(mip, uv + vec2(0.0,  3.23076923) * pxSize).rgb;
    
    // sum weighted color
    vec3 color =
        c0 * 0.07027027 + c1 * 0.31621622
        + c2 * 0.22702703
        + c3 * 0.31621622 + c4 * 0.07027027;
    
    return color;
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    // Calculate UV and px size
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    vec2 pxSize = 1.0 / vec2(size);
    
    // Sample color
    vec3 color = vec3(0.0);
    if (uIsVertical) {
        color = blurV(uSourceSampler, uv, pxSize);
    } else {
        color = blurH(uSourceSampler, uv, pxSize);
    }
    
    // Store the result
    imageStore(uOutputImage, coords, vec4(color, 1.0));
}