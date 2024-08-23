#version 460 core

layout(local_size_x = 8, local_size_y = 8) in;

layout(binding = 0) uniform sampler2D uSamplerInput;
layout(rgba16f, binding = 1) uniform image2D uImageOutput;

uniform ivec2 uOutputSize;

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
    //  J K
    // D E F
    //  L M
    // G H I
    // Fetch colors according to the pattern above
    vec4 a = texture(uSamplerInput, uv + vec2(-pxSize.x, -pxSize.y)      );
    vec4 b = texture(uSamplerInput, uv + vec2( 0.0,      -pxSize.y)      );
    vec4 c = texture(uSamplerInput, uv + vec2( pxSize.x, -pxSize.y)      );
    vec4 d = texture(uSamplerInput, uv + vec2(-pxSize.x,  0.0     )      );
    vec4 e = texture(uSamplerInput, uv                                   );
    vec4 f = texture(uSamplerInput, uv + vec2( pxSize.x,  0.0     )      );
    vec4 g = texture(uSamplerInput, uv + vec2(-pxSize.x,  pxSize.y)      );
    vec4 h = texture(uSamplerInput, uv + vec2( 0.0,       pxSize.y)      );
    vec4 i = texture(uSamplerInput, uv + vec2( pxSize.x,  pxSize.y)      );
    vec4 j = texture(uSamplerInput, uv + vec2(-pxSize.x, -pxSize.y) * 0.5);
    vec4 k = texture(uSamplerInput, uv + vec2( pxSize.x, -pxSize.y) * 0.5);
    vec4 l = texture(uSamplerInput, uv + vec2(-pxSize.x,  pxSize.y) * 0.5);
    vec4 m = texture(uSamplerInput, uv + vec2( pxSize.x,  pxSize.y) * 0.5);
    
    // Apply bloom downsample
    vec4 color = vec4(0.0);
    color += (a + b + d + e) * 0.125;
    color += (b + c + e + f) * 0.125;
    color += (d + e + g + h) * 0.125;
    color += (e + f + h + i) * 0.125;
    color += (j + k + l + m) * 0.5;
    color *= 1.0 / 4.0;
    
    // Store result
    imageStore(uImageOutput, coords, color);
}
