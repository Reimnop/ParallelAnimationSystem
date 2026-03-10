#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uTexture;
uniform sampler2D uNoiseTexture;

uniform highp float uIntensity;
uniform highp float uColorIntensity;

in highp vec2 vUv;

vec4 glitch(sampler2D tex, sampler2D noiseTex, vec2 uv, float intensity, float colorIntensity) {
    vec3 glitch = texture(noiseTex, uv).xyz;
    float thresh = 1.001 - intensity * 1.001;
    float w_d = step(thresh * colorIntensity, pow(abs(glitch.z), 2.5));
    float w_c = step(thresh, pow(abs(glitch.z), 3.5));
    vec2 uv2 = fract(uv + glitch.xy * w_d);
    vec4 source = texture(tex, uv2);
    vec3 color = source.rgb;
    vec3 neg = clamp(color.grb + (1.0 - dot(color, vec3(1.0))) * 0.1, 0.0, 1.0);
    color = mix(color, neg, w_c);
    return vec4(color, source.a);
}

void main() {
    vec2 uv = vUv;
    oFragColor = glitch(uTexture, uNoiseTexture, uv, uIntensity, uColorIntensity);
}
