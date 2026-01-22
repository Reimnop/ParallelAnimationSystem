#version 300 es

precision highp float;

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUv;
layout(location = 2) in vec4 aColor;

uniform vec2 uScale;

out vec2 vUv;
out vec4 vColor;

void main() {
    vUv = aUv;
    vColor = aColor;
    
    vec2 pos01 = aPos * uScale;
    
    // Flip Y for OpenGL
    pos01.y = 1.0 - pos01.y;
    
    // Convert to clip space
    vec2 clipPos = pos01 * 2.0 - 1.0;
    
    gl_Position = vec4(clipPos, 0.0, 1.0);
}
