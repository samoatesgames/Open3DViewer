#version 450

layout(location=0) in Vertex
{
	vec3 color;
} vin;

layout(location = 0) out vec4 fsout_color;

void main()
{
    fsout_color = vec4(vin.color, 1);
}