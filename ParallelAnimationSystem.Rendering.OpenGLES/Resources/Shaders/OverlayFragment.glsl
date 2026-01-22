#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

in highp vec2 vUv;

uniform sampler2D uSourceSampler;

void main() {
    // Discard OOB fragments
    if (vUv.x > 1.0 || vUv.y > 1.0) {
        discard;
    }
    
    oFragColor = texture(uSourceSampler, vUv);
}