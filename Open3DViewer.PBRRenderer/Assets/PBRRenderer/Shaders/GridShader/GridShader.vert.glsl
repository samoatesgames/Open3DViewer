#version 450
layout(set = 0, binding = 0) uniform ViewProjectionBuffer
{
    mat4 View;
    mat4 Projection;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Color;

layout(location=0) out Vertex
{
	vec3 color;
} vout;

void main()
{
    vec4 worldPosition = mat4(1) * vec4(Position, 1);
    vout.color = Color;
	gl_Position = Projection * (View * worldPosition);
}