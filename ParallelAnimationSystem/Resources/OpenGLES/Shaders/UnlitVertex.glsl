#version 300 es

precision highp float;

layout(location = 0) in highp vec2 aPos;

out highp vec2 vUv;

uniform highp mat3 uMvp;
uniform highp float uZ;

void main() {
    // Since aPos is always in the range [-0.5, 0.5],
    // we can use it to directly calculate UVs,
    // without wasting space in the vertex buffer
    vUv = aPos + vec2(0.5);

    gl_Position = vec4(vec2(uMvp * vec3(aPos, 1.0)), uZ, 1.0);
}