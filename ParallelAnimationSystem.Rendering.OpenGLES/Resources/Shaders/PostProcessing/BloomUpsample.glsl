#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;
uniform float uSampleScale;

in highp vec2 vUv;

vec3 sample9Tap(sampler2D mip, vec2 uv, vec2 radius) {
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
    color *= 0.0625;

    return color;
}

void main() {
    ivec2 size = textureSize(uSourceSampler, 0);
    vec2 pxSize = 1.0 / vec2(size);
    
    // Sample color
    vec3 color = sample9Tap(uSourceSampler, vUv, pxSize * uSampleScale);
    
    // Store result
    // We'll use GL blend to accumulate the result
    // So just output the sampled color
    oFragColor = vec4(color, 1.0);
}