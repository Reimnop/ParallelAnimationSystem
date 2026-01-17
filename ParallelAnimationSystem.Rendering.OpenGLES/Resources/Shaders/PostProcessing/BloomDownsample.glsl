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
        e * 0.125 +
        (a + c + g + i) * 0.03125 +
        (b + d + f + h) * 0.0625 +
        (j + k + l + m) * 0.125;
    
    // Store result
    oFragColor = vec4(color, 1.0);
}