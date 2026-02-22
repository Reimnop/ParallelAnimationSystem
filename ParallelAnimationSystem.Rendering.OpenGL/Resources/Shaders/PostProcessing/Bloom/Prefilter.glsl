#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

const float EPSILON = 0.0001;

layout(rgba16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;

uniform float uThreshold;
uniform vec3 uCurve;

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
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);
    
    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;
    
    // Calculate uv
    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    
    // Load color
    vec3 color = texture(uSourceSampler, uv).rgb;
    
    // Apply threshold
    color = quadraticThreshold(color, uThreshold, uCurve);
    
    // Store result
    imageStore(uOutputImage, coords, vec4(color, 1.0));
}