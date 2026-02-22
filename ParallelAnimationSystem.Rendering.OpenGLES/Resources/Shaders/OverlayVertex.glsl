#version 300 es

precision highp float;

const highp vec2 VERTICES[] = vec2[](
    vec2(0.0, 0.0),
    vec2(2.0, 0.0),
    vec2(0.0, 2.0)
);

out highp vec2 vUv;

uniform vec2 uOffset;
uniform vec2 uScale;

void main() {
    vUv = VERTICES[gl_VertexID];
    
    vec2 pos = VERTICES[gl_VertexID] * uScale + uOffset;
    gl_Position = vec4(pos * 2.0 - 1.0, 0.0, 1.0);
}