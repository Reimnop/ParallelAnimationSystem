#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uLowMip;
uniform sampler2D uHighMip;

uniform ivec2 uSize;
uniform highp float uDiffusion;

in highp vec2 vUv;

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
    vec2 pxSize = 1.0 / vec2(uSize);
    
    // Fetch current color
    vec3 lowMipColor = sampleLowMip(uLowMip, vUv, pxSize * 0.5);
    
    // Fetch original color
    vec3 highMipColor = texture(uHighMip, vUv).rgb;
    
    // Mix
    vec3 color = mix(highMipColor, lowMipColor, uDiffusion);
    
    // Store result
    oFragColor = vec4(color, 1.0);
}