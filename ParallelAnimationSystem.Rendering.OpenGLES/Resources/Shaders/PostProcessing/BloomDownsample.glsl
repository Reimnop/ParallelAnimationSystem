#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;

in highp vec2 vUv;

vec3 sample13Tap(sampler2D mip, vec2 uv, vec2 radius) {
    radius *= 0.5;

    // A B C
    //  J K
    // D E F
    //  L M
    // G H I
    vec3 a = texture(mip, uv + vec2(-2.0, -2.0) * radius).rgb;
    vec3 b = texture(mip, uv + vec2( 0.0, -2.0) * radius).rgb;
    vec3 c = texture(mip, uv + vec2( 2.0, -2.0) * radius).rgb;
    vec3 d = texture(mip, uv + vec2(-2.0,  0.0) * radius).rgb;
    vec3 e = texture(mip, uv + vec2( 0.0,  0.0) * radius).rgb;
    vec3 f = texture(mip, uv + vec2( 2.0,  0.0) * radius).rgb;
    vec3 g = texture(mip, uv + vec2(-2.0,  2.0) * radius).rgb;
    vec3 h = texture(mip, uv + vec2( 0.0,  2.0) * radius).rgb;
    vec3 i = texture(mip, uv + vec2( 2.0,  2.0) * radius).rgb;
    vec3 j = texture(mip, uv + vec2(-1.0, -1.0) * radius).rgb;
    vec3 k = texture(mip, uv + vec2( 1.0, -1.0) * radius).rgb;
    vec3 l = texture(mip, uv + vec2(-1.0,  1.0) * radius).rgb;
    vec3 m = texture(mip, uv + vec2( 1.0,  1.0) * radius).rgb;

    vec3 color =
        e * 0.125 +
        (a + c + g + i) * 0.03125 +
        (b + d + f + h) * 0.0625 +
        (j + k + l + m) * 0.125;

    return color;
}

void main() {
    // Get size
    ivec2 size = textureSize(uSourceSampler, 0);
    
    // Calculate px size
    vec2 pxSize = 1.0 / vec2(size);
    
    // Sample color
    vec3 color = sample13Tap(uSourceSampler, vUv, pxSize);
    
    // Store result
    oFragColor = vec4(color, 1.0);
}