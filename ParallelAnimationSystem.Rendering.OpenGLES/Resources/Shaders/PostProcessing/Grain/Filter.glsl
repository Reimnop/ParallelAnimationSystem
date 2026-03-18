#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;

uniform highp vec2 uParams; // x = b, y = c

in highp vec2 vUv;

float applyFilter(vec2 uv, vec2 texelSize, float b, float c) {
    return (1.0 / (4.0 + b * 4.0 + abs(c))) * (
        texture(uSourceSampler, uv + vec2(-1.0, -1.0) * texelSize).r +
        texture(uSourceSampler, uv + vec2( 0.0, -1.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2( 1.0, -1.0) * texelSize).r +
        texture(uSourceSampler, uv + vec2(-1.0,  0.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2( 0.0,  0.0) * texelSize).r * c +
        texture(uSourceSampler, uv + vec2( 1.0,  0.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2(-1.0,  1.0) * texelSize).r +
        texture(uSourceSampler, uv + vec2( 0.0,  1.0) * texelSize).r * b +
        texture(uSourceSampler, uv + vec2( 1.0,  1.0) * texelSize).r
    );
}

void main() {
    vec2 texelSize = 1.0 / vec2(textureSize(uSourceSampler, 0));
    float n = applyFilter(vUv, texelSize, uParams.x, uParams.y);

    oFragColor = vec4(vec3(n), 1.0);
}