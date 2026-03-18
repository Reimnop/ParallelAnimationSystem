#version 300 es

precision highp float;

const highp vec3 NOISE_PARAMS = vec3(12.9898, 78.233, 43758.5453);

layout(location = 0) out highp vec4 oFragColor;

uniform highp float uPhase;

in highp vec2 vUv;

float noise(vec2 uv, float phase) {
    vec2 n = uv + phase;
    return fract(sin(dot(n, NOISE_PARAMS.xy)) * NOISE_PARAMS.z);
}

void main() {
    float n = noise(vUv, uPhase);
    oFragColor = vec4(vec3(n), 1.0);
}