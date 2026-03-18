#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;

uniform int uChannel;

in highp vec2 vUv;

void main() {
    float noise = texture(uSourceSampler, vUv).r;
    vec3 color;
    color[uChannel] = noise;
    oFragColor = vec4(color, 1.0);
}