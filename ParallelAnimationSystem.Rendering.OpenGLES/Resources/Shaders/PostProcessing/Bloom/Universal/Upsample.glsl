#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;
uniform float uScatter;

in highp vec2 vUv;

void main() {
    // Sample color
    vec3 color = texture(uSourceSampler, vUv).rgb;
    
    // Store result
    // We'll use GL blend to blend the result
    // So just output the sampled color
    oFragColor = vec4(color, uScatter);
}