#version 460 core

layout(location = 0) in vec2 aPos;

out vec2 vUv;

uniform mat3 uMvp;
uniform float uZ;

void main() {
    // Since aPos is always in the range [-0.5, 0.5],
    // we can use it to directly calculate UVs,
    // without wasting space in the vertex buffer
    vUv = aPos + vec2(0.5);
    
    gl_Position = vec4(vec2(uMvp * vec3(aPos, 1.0)), uZ, 1.0);
}