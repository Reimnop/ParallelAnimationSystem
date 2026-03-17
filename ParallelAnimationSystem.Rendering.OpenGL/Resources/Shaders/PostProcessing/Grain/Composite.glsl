#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(r16f, binding = 0) uniform image2D uOutputImage;

uniform sampler2D uSourceSampler;
uniform sampler2D uNoiseSampler;

uniform bool uColored;
uniform float uLuminanceContribution;
uniform float uIntensity;
uniform vec2 uScale;
uniform vec2 uOffset;

vec3 gammaToLinear(vec3 color) {
    return color * color;
}

vec3 linearToGamma(vec3 color) {
    return sqrt(color);
}

float luminance(vec3 color) {
    return dot(color, vec3(0.2126729, 0.7151522, 0.0721750));
}

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(uOutputImage);

    // Skip if out of bounds
    if (coords.x >= size.x || coords.y >= size.y)
        return;

    vec2 uv = vec2(coords.x + 0.5, coords.y + 0.5) / vec2(size);
    
    vec3 color = gammaToLinear(texture(uSourceSampler, uv).rgb);
    
    vec4 noiseRaw = texture(uNoiseSampler, uv * uScale + uOffset);
    vec3 grain = uColored ? noiseRaw.rgb : vec3(noiseRaw.r);
    
    float lum = 1.0 - sqrt(luminance(clamp(color, 0.0, 1.0)));
    lum = mix(1.0, lum, uLuminanceContribution);
    
    color += color * grain * lum * uIntensity;
    
    color = linearToGamma(color);

    imageStore(uOutputImage, coords, vec4(color, 1.0));
}