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
layout(location = 2) in vec3 Tangent;
layout(location = 3) in vec3 BiTangent;
layout(location = 4) in vec2 TexCoords;
layout(location = 5) in vec4 Color;

layout(location=0) out Vertex
{
	vec3 position;
	vec3 normal;
	vec3 tangent;
	vec3 bitangent;
	vec2 texcoord;
	vec4 color;
	mat3 tangentBasis;
} vout;

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);

    vout.position = worldPosition.xyz;
    vout.normal = Normal;
    vout.tangent = Tangent;
    vout.bitangent = BiTangent;
	vout.texcoord = TexCoords;
	vout.color = Color;
	vout.tangentBasis = mat3(World) * mat3(Tangent, BiTangent, Normal);

	gl_Position = Projection * (View * worldPosition);
}