#version 450

layout(location = 0) in vec2 fsin_texCoords;
layout(location = 0) out vec4 fsout_color;

layout(set = 2, binding = 1) uniform sampler DiffuseSampler;
layout(set = 2, binding = 2) uniform texture2D DiffuseTexture;

layout(set = 3, binding = 1) uniform sampler NormalSampler;
layout(set = 3, binding = 2) uniform texture2D NormalTexture;

layout(set = 4, binding = 1) uniform sampler MetallicRoughnessSampler;
layout(set = 4, binding = 2) uniform texture2D MetallicRoughnessTexture;

layout(set = 5, binding = 1) uniform sampler EmissiveSampler;
layout(set = 5, binding = 2) uniform texture2D EmissiveTexture;

layout(set = 6, binding = 1) uniform sampler OcclusionSampler;
layout(set = 6, binding = 2) uniform texture2D OcclusionTexture;

void main()
{
    vec4 diffuse = texture(sampler2D(DiffuseTexture, DiffuseSampler), fsin_texCoords);
    vec4 normal = texture(sampler2D(NormalTexture, NormalSampler), fsin_texCoords);
    vec4 roughness = texture(sampler2D(MetallicRoughnessTexture, MetallicRoughnessSampler), fsin_texCoords);
    vec4 emissive = texture(sampler2D(EmissiveTexture, EmissiveSampler), fsin_texCoords);
    vec4 occlusion = texture(sampler2D(OcclusionTexture, OcclusionSampler), fsin_texCoords);

    //fsout_color = diffuse + normal + roughness + emissive + occlusion;
    fsout_color = diffuse;
}