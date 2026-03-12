#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uTexture;
uniform sampler2D uNoiseTexture;

uniform highp float uIntensity;
uniform highp float uColorIntensity;

in highp vec2 vUv;

void main() {
    vec4 glitch = texture(uNoiseTexture, vUv);
    float activeAmount = pow(uIntensity, 1.8); 
    float thresh = 1.0 - activeAmount;
    float w_c = step(thresh * 0.99, glitch.z); 
    float w_d = step(thresh, glitch.z);
    float scrambleScale = mix(0.04, 0.30, uIntensity);
    vec2 offset = (glitch.xy - 0.5) * scrambleScale;
    vec2 uv = fract(vUv + offset * w_d);
    vec4 source = texture(uTexture, uv);
    vec3 color = source.rgb;
    vec3 neg = clamp(color.grb + (1.0 - dot(color, vec3(1.0))) * 0.1, 0.0, 1.0);
    color = mix(color, neg, w_c);
    oFragColor = vec4(color, source.a);
}
