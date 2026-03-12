#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;
uniform sampler2D uBloomSampler;

uniform highp vec3 uTint;

in highp vec2 vUv;

void main() {
    vec3 srcColor = texture(uSourceSampler, vUv).rgb;
    vec3 bloomColor = texture(uBloomSampler, vUv).rgb;
    
    srcColor *= srcColor; // Convert to linear space
    
    // We don't convert bloomColor because it's already in linear space
    vec3 color = srcColor + bloomColor * uTint;
    
    // Convert back to gamma space
    color = sqrt(color);
    
    // Store result
    oFragColor = vec4(color, 1.0);
}