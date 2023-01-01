#version 450
layout(set = 0, binding = 0) uniform ViewProjectionBuffer
{
    mat4 View;
    mat4 Projection;
};

layout(set = 1, binding = 0) uniform WorldBuffer
{
    mat4 World;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TexCoords;

layout(location = 0) out vec3 fsin_normal;
layout(location = 1) out vec2 fsin_texCoords;

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    
    fsin_normal = Normal;
    fsin_texCoords = TexCoords;
}