#version 300 es

precision highp float;

const float EPSILON = 0.0001;

layout(location = 0) out highp vec4 oFragColor;

uniform sampler2D uSourceSampler;

uniform float uThreshold;
uniform vec3 uCurve;

in highp vec2 vUv;

// "Borrowed" from Unity URP
// curve = (threshold - knee, knee * 2, 0.25 / knee)
vec3 quadraticThreshold(vec3 color, float threshold, vec3 curve) {
    float br = max(color.r, max(color.g, color.b));

    // Under-threshold part: quadratic curve
    float rq = clamp(br - curve.x, 0.0, curve.y);
    rq = curve.z * rq * rq;

    // Combine and apply the brightness response curve.
    color *= max(rq, br - threshold) / max(br, EPSILON);

    return color;
}

void main() {
    // Load color
    vec3 color = texture(uSourceSampler, vUv).rgb;
    
    // Apply threshold
    color = quadraticThreshold(color, uThreshold, uCurve);
    
    // Store result
    oFragColor = vec4(color, 1.0);
}