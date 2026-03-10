#version 300 es

precision highp float;

layout(location = 0) in highp vec2 aPos;

out highp vec2 vUv;

uniform highp mat3x2 uMvp;
uniform highp float uZ;
uniform highp float uGradientRotation;
uniform highp float uGradientScale;

void main() {
    // rotate UV
    float c = cos(uGradientRotation) * uGradientScale;
    float s = sin(uGradientRotation) * uGradientScale;
    mat2 uvTransform = mat2(c, -s, s, c);
    
    vUv = uvTransform * aPos + vec2(0.5);

    gl_Position = vec4(vec2(uMvp * vec3(aPos, 1.0)), uZ, 1.0);
}