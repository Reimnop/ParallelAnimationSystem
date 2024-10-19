#version 300 es

precision highp float;

const float[] kernel = float[](
    0.01621622, 0.05405405, 0.12162162, 0.19459459,
    0.22702703,
    0.19459459, 0.12162162, 0.05405405, 0.01621622
);

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uTexture;

uniform highp ivec2 uSize;
uniform bool uVertical;

in highp vec2 vUv;

void main() {
    // Calculate px size
    vec2 pxSize = 1.0 / vec2(uSize);

    // A B C
    //  J K
    // D E F
    //  L M
    // G H I
    vec3 a = texture(uTexture, vUv + vec2(-1.0, -1.0) * pxSize).rgb;
    vec3 b = texture(uTexture, vUv + vec2( 0.0, -1.0) * pxSize).rgb;
    vec3 c = texture(uTexture, vUv + vec2( 1.0, -1.0) * pxSize).rgb;
    vec3 d = texture(uTexture, vUv + vec2(-1.0,  0.0) * pxSize).rgb;
    vec3 e = texture(uTexture, vUv + vec2( 0.0,  0.0) * pxSize).rgb;
    vec3 f = texture(uTexture, vUv + vec2( 1.0,  0.0) * pxSize).rgb;
    vec3 g = texture(uTexture, vUv + vec2(-1.0,  1.0) * pxSize).rgb;
    vec3 h = texture(uTexture, vUv + vec2( 0.0,  1.0) * pxSize).rgb;
    vec3 i = texture(uTexture, vUv + vec2( 1.0,  1.0) * pxSize).rgb;
    vec3 j = texture(uTexture, vUv + vec2(-0.5, -0.5) * pxSize).rgb;
    vec3 k = texture(uTexture, vUv + vec2( 0.5, -0.5) * pxSize).rgb;
    vec3 l = texture(uTexture, vUv + vec2(-0.5,  0.5) * pxSize).rgb;
    vec3 m = texture(uTexture, vUv + vec2( 0.5,  0.5) * pxSize).rgb;

    vec3 color =
        (a + b + d + e) * 0.125 +
        (b + c + e + f) * 0.125 +
        (d + e + g + h) * 0.125 +
        (e + f + h + i) * 0.125 +
        (j + k + l + m) * 0.5;
    color *= 0.25;
    
    // Store result
    oFragColor = vec4(color, 1.0);
}