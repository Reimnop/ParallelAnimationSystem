#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(binding = 0) uniform sampler2D uSamplerInput;
layout(rgba16f, binding = 1) uniform image2D uImageOutput;

uniform ivec2 uOutputSize;
uniform float uDiffusion;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    // Skip if out of bounds
    if (coords.x >= uOutputSize.x || coords.y >= uOutputSize.y)
    return;

    // Calculate UV and pixel size
    vec2 uv = (vec2(coords) + 0.5) / vec2(uOutputSize);
    vec2 pxSize = 1.0 / vec2(uOutputSize);

    // Apply bloom downsample
    // Reference: https://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare/
    // A B C
    // D E F
    // G H I
    // Fetch colors according to the pattern above
    vec4 a = texture(uSamplerInput, uv + vec2(-pxSize.x, -pxSize.y) * 0.5);
    vec4 b = texture(uSamplerInput, uv + vec2( 0.0,      -pxSize.y) * 0.5);
    vec4 c = texture(uSamplerInput, uv + vec2( pxSize.x, -pxSize.y) * 0.5);
    vec4 d = texture(uSamplerInput, uv + vec2(-pxSize.x,  0.0     ) * 0.5);
    vec4 e = texture(uSamplerInput, uv                                   );
    vec4 f = texture(uSamplerInput, uv + vec2( pxSize.x,  0.0     ) * 0.5);
    vec4 g = texture(uSamplerInput, uv + vec2(-pxSize.x,  pxSize.y) * 0.5);
    vec4 h = texture(uSamplerInput, uv + vec2( 0.0,       pxSize.y) * 0.5);
    vec4 i = texture(uSamplerInput, uv + vec2( pxSize.x,  pxSize.y) * 0.5);
    
    // Apply bloom upsample
    vec4 color = vec4(0.0);
    color += (a + c + g + i) * 1.0;
    color += (b + d + f + h) * 2.0;
    color += e               * 4.0;
    color *= 1.0 / 16.0;
    
    // Fetch original color
    vec4 highMipColor = imageLoad(uImageOutput, coords);
    
    // Mix with current color
    color = mix(highMipColor, color, uDiffusion);
    
    // Store result
    imageStore(uImageOutput, coords, color);
}
