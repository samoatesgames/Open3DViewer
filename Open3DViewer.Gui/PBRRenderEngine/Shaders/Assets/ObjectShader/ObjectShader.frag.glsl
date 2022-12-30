#version 450

layout(location = 0) in vec3 fsin_normal;
layout(location = 1) in vec2 fsin_texCoords;

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

float lambert(vec3 N, vec3 L)
{
  vec3 nrmN = normalize(N);
  vec3 nrmL = normalize(L);
  float result = dot(nrmN, nrmL);
  return max(result, 0.0);
}

void main()
{
    vec4 diffuseTexture = texture(sampler2D(DiffuseTexture, DiffuseSampler), fsin_texCoords);
    vec4 normalTexture = texture(sampler2D(NormalTexture, DiffuseSampler), fsin_texCoords);
    vec4 roughnessTexture = texture(sampler2D(MetallicRoughnessTexture, MetallicRoughnessSampler), fsin_texCoords);
    vec4 emissiveTexture = texture(sampler2D(EmissiveTexture, EmissiveSampler), fsin_texCoords);
    vec4 occlusionTexture = texture(sampler2D(OcclusionTexture, OcclusionSampler), fsin_texCoords);

    // TODO: Provide lights from buffers
    vec3 lightDirection = vec3(-2,1,-4);
    vec3 lightDiffuse = vec3(1.0, 1.0, 1.0);
    vec3 lighting = lightDiffuse * lambert(fsin_normal, lightDirection);

    // TODO: Provide ambiant amount from buffer
    vec3 ambiantLight = vec3(0.4, 0.4, 0.4);

    vec3 result = (clamp(ambiantLight + lighting, 0, 1) * diffuseTexture.xyz);       
    fsout_color = vec4(result, 1.0f);
}