#version 460 core

layout(location = 0) in vec2 aPos;

out vec2 vUv;
out vec4 vColor1;
out vec4 vColor2;
out flat int vRenderMode;

struct MultiDrawItem {
    mat3 mvp;
    vec4 color1;
    vec4 color2;
    float z;
    int renderMode;
};

layout(std430, binding = 0) buffer MultiDraw {
    MultiDrawItem multiDrawItems[];
};

void main() {
    // Since aPos is always in the range [-0.5, 0.5],
    // we can use it to directly calculate UVs,
    // without wasting space in the vertex buffer
    vUv = aPos + vec2(0.5);
    
    // Get the data for this draw call
    MultiDrawItem item = multiDrawItems[gl_DrawID];
    
    // Pass the data to the fragment shader
    vColor1 = item.color1;
    vColor2 = item.color2;
    vRenderMode = item.renderMode;
    
    // Calculate the vertex position
    gl_Position = vec4(vec2(item.mvp * vec3(aPos, 1.0)), item.z, 1.0);
}