#version 300 es

precision highp float;

const highp vec2 VERTICES[] = vec2[](
    vec2(0.0, 0.0),
    vec2(2.0, 0.0),
    vec2(0.0, 2.0)
);

out highp vec2 vUv;

void main() {
    vUv = VERTICES[gl_VertexID];
    gl_Position = vec4(VERTICES[gl_VertexID] * 2.0 - 1.0, 0.0, 1.0);
}