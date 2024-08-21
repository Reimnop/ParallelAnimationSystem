#version 460 core

layout(location = 0) out vec4 fragColor;

layout(location = 2) uniform vec4 uColor;

void main() {
    fragColor = uColor;
}