#version 460 core

layout(location = 0) out vec4 oFragColor;

uniform sampler2D uTexture;

in vec2 vUv;
in vec4 vColor;

void main() {
    vec4 texColor = texture(uTexture, vUv);
    oFragColor = vColor * texColor;
}
