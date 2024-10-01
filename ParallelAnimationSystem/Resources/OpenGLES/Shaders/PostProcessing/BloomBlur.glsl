#version 300 es

precision highp float;

const highp float[] kernel = float[](
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
    vec2 px = 1.0 / vec2(uSize);
    
    // Add all the colors
    vec4 color = vec4(0.0);
    for (int i = 0; i < 9; i++) {
        vec2 currentUv = uVertical 
            ? vUv + vec2(0, -4 + i) * px 
            : vUv + vec2(-4 + i, 0) * px;
        vec4 currentColor = texture(uTexture, currentUv);
        color += currentColor * kernel[i];
    }
    
    // Store result
    oFragColor = color;
}