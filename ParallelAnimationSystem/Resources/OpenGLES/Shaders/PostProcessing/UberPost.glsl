#version 300 es

precision highp float;

layout(location = 0) out highp vec4 oFragColor;

const float PI = 3.14159265359;

uniform sampler2D uTexture;

uniform highp ivec2 uSize;

uniform highp float uTime;

uniform highp float uHueShiftAngle;

uniform highp float uLensDistortionIntensity;
uniform highp vec2 uLensDistortionCenter;

uniform highp float uChromaticAberrationIntensity;

uniform highp vec2 uVignetteCenter;
uniform highp float uVignetteIntensity;
uniform highp float uVignetteRounded;
uniform highp float uVignetteRoundness;
uniform highp float uVignetteSmoothness;
uniform highp vec3 uVignetteColor;

uniform highp vec3 uGradientColor1;
uniform highp vec3 uGradientColor2;
uniform highp float uGradientIntensity;
uniform highp mat2 uGradientRotation;
uniform highp int uGradientMode;

uniform highp float uGlitchIntensity;
uniform highp float uGlitchSpeed;
uniform highp vec2 uGlitchSize;

in highp vec2 vUv;

// "Borrowed" from https://gist.github.com/mairod/a75e7b44f68110e1576d77419d608786
vec3 hueShift(vec3 color, float hueAdjust) {
    const vec3 kRGBToYPrime = vec3(0.299, 0.587, 0.114);
    const vec3 kRGBToI      = vec3(0.596, -0.275, -0.321);
    const vec3 kRGBToQ      = vec3(0.212, -0.523, 0.311);

    const vec3 kYIQToR = vec3(1.0, 0.956, 0.621);
    const vec3 kYIQToG = vec3(1.0, -0.272, -0.647);
    const vec3 kYIQToB = vec3(1.0, -1.107, 1.704);

    float YPrime  = dot(color, kRGBToYPrime);
    float I       = dot(color, kRGBToI);
    float Q       = dot(color, kRGBToQ);
    float hue     = atan(Q, I);
    float chroma  = sqrt(I * I + Q * Q);

    hue += hueAdjust;

    Q = chroma * sin(hue);
    I = chroma * cos(hue);

    vec3 yIQ = vec3(YPrime, I, Q);

    return vec3(dot(yIQ, kYIQToR), dot(yIQ, kYIQToG), dot(yIQ, kYIQToB));
}

// "Borrowed" from Unity's PostProcessing package
vec2 distortLens(vec2 uv) {
    float intensity = uLensDistortionIntensity;
    vec2 center = uLensDistortionCenter;

    if (intensity == 0.0)
    return uv;

    float amount = 1.6 * max(abs(intensity), 1.0);
    float theta = min(160.0, amount) * PI / 180.0;
    float sigma = 2.0 * tan(theta * 0.5);

    vec2 ruv = uv - vec2(0.5) - center;
    float ru = length(ruv);

    if (intensity > 0.0) {
        float wu = ru * theta;
        ru = tan(wu) * (1.0 / (ru * sigma));
        uv = uv + ruv * (ru - 1.0);
    } else {
        ru = (1.0 / ru) * (1.0 / theta) * atan(ru * sigma);
        uv = uv + ruv * (ru - 1.0);
    }

    return uv;
}

vec3 vignette(vec3 color, vec2 uv, vec2 center, float aspect, float intensity, float rounded, float roundness, float smoothness, vec3 vignetteColor) {
    vec2 dist = abs(uv - center) * intensity;
    dist.x *= mix(1.0, aspect, rounded);
    dist = pow(clamp(dist, 0.0, 1.0), vec2(roundness));
    float vFactor = pow(clamp(1.0 - dot(dist, dist), 0.0, 1.0), smoothness);
    return color * mix(vignetteColor, vec3(1.0), vFactor);
}

vec3 applyGradient(vec3 color, vec2 uv, vec3 color1, vec3 color2, float intensity, mat2 rotation, int mode) {
    if (intensity == 0.0)
        return color;

    vec2 uvNormalized = uv - 0.5;
    uvNormalized = rotation * uvNormalized;
    float gradient = uvNormalized.y + 0.5;
    vec3 gradientColor = mix(color2, color1, gradient);

    if (mode == 0) // linear
        return mix(color, gradientColor, intensity);
    else if (mode == 1) // additive
        return color + gradientColor * intensity;
    else if (mode == 2) // multiplicative
        return color * mix(vec3(1.0), gradientColor, intensity);
    else if (mode == 3) // screen
        return vec3(1.0) - (vec3(1.0) - color) * (vec3(1.0) - gradientColor * intensity);
    else // default to linear
        return mix(color, gradientColor, intensity);
}

// https://stackoverflow.com/a/17479300/13455707
// A single iteration of Bob Jenkins' One-At-A-Time hashing algorithm.
uint hash( uint x ) {
    x += ( x << 10u );
    x ^= ( x >>  6u );
    x += ( x <<  3u );
    x ^= ( x >> 11u );
    x += ( x << 15u );
    return x;
}



// Compound versions of the hashing algorithm I whipped together.
uint hash( uvec2 v ) { return hash( v.x ^ hash(v.y)                         ); }
uint hash( uvec3 v ) { return hash( v.x ^ hash(v.y) ^ hash(v.z)             ); }
uint hash( uvec4 v ) { return hash( v.x ^ hash(v.y) ^ hash(v.z) ^ hash(v.w) ); }



// Construct a float with half-open range [0:1] using low 23 bits.
// All zeroes yields 0.0, all ones yields the next smallest representable value below 1.0.
float floatConstruct( uint m ) {
    const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
    const uint ieeeOne      = 0x3F800000u; // 1.0 in IEEE binary32

    m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
    m |= ieeeOne;                          // Add fractional part to 1.0

    float  f = uintBitsToFloat( m );       // Range [1:2]
    return f - 1.0;                        // Range [0:1]
}



// Pseudo-random value in half-open range [0:1].
float random( float x ) { return floatConstruct(hash(floatBitsToUint(x))); }
float random( vec2  v ) { return floatConstruct(hash(floatBitsToUint(v))); }
float random( vec3  v ) { return floatConstruct(hash(floatBitsToUint(v))); }
float random( vec4  v ) { return floatConstruct(hash(floatBitsToUint(v))); }

vec3 glitchColor(vec3 color, float r) {
    vec3 a = clamp(color.grb + (1.0 - (color.r + color.g + color.b)) * 0.1, 0.0, 1.0);
    return mix(color, a, pow(r, 3.0));
}

vec4 sampleTextureGlitched(sampler2D tex, vec2 uv, float t, float intensity, float speed, vec2 size) {
    if (intensity == 0.0)
    return texture(tex, uv);

    const float scrambleIntensity = 0.1;

    vec2 noiseUv = floor(uv / size) * size;

    t = floor(t * speed) / speed;

    float r1 = random(vec4(noiseUv, t, 0.0));
    float r2 = random(vec4(noiseUv, t, 1.0));
    float r3 = random(vec4(noiseUv, t, 2.0));
    float r4 = random(vec4(noiseUv, t, 3.0));
    float r5 = random(vec4(noiseUv, t, 4.0));

    vec2 rUv = r2 < scrambleIntensity ? floor(vec2(r3, r4) / size) * size + (uv - noiseUv) : uv;

    vec4 uvColor = texture(tex, uv);
    vec4 rUvColor = texture(tex, rUv);

    return r1 < intensity ? vec4(glitchColor(rUvColor.rgb, r5), rUvColor.a) : uvColor;
}

vec3 sampleTexture(sampler2D tex, vec2 uv) {
    vec3 color = sampleTextureGlitched(tex, uv, uTime, uGlitchIntensity, uGlitchSpeed, uGlitchSize).rgb;
    color = applyGradient(color, uv, uGradientColor1, uGradientColor2, uGradientIntensity, uGradientRotation, uGradientMode);
    return color;
}

void main() {
    vec2 uv = vUv;
    
    // Apply lens distortion
    vec2 uvDistorted = distortLens(uv);

    // Apply chromatic aberration
    vec3 color;
    if (uChromaticAberrationIntensity == 0.0) {
        color = sampleTexture(uTexture, uvDistorted);
    } else {
        vec2 coords = uv * 2.0 - 1.0;
        vec2 end = uv - coords * dot(coords, coords) * uChromaticAberrationIntensity;
        vec2 delta = (end - uv) / 3.0;

        float r = sampleTexture(uTexture, uvDistorted).r;
        float g = sampleTexture(uTexture, distortLens(uv + delta)).g;
        float b = sampleTexture(uTexture, distortLens(uv + delta * 2.0)).b;

        color = vec3(r, g, b);
    }

    // Apply vignette
    if (uVignetteIntensity != 0.0) {
        color = vignette(color, uvDistorted, uVignetteCenter, float(uSize.x) / float(uSize.y), uVignetteIntensity, uVignetteRounded, uVignetteRoundness, uVignetteSmoothness, uVignetteColor);
    }

    // Hue shift
    if (uHueShiftAngle != 0.0) {
        color = hueShift(color, uHueShiftAngle);
    }
    
    // Store result
    oFragColor = vec4(color, 1.0);
}