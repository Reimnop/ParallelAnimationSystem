#version 460 core

layout(location = 0) in vec2 aPos;

layout(location = 0) uniform mat3 uMvp;
layout(location = 1) uniform float uZ;

void main() {
    gl_Position = vec4(vec2(uMvp * vec3(aPos, 1.0)), uZ, 1.0);
}