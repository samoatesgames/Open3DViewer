#version 450

layout(location = 0) in vec3 fsin_normal;
layout(location = 1) in vec2 fsin_texCoords;

layout(location = 0) out vec4 fsout_color;

layout(set = 2, binding = 0) uniform sampler DiffuseSampler;
layout(set = 2, binding = 1) uniform texture2D DiffuseTexture;
layout(set = 2, binding = 2) uniform DiffuseMaterial
{
    vec4 DiffuseTint;
};

layout(set = 3, binding = 0) uniform sampler NormalSampler;
layout(set = 3, binding = 1) uniform texture2D NormalTexture;
layout(set = 3, binding = 2) uniform NormalMaterial
{
    vec4 NormalTint;
};

float lambert(vec3 N, vec3 L)
{
  vec3 nrmN = normalize(N);
  vec3 nrmL = normalize(L);
  float result = dot(nrmN, nrmL);
  return max(result, 0.0);
}

void main()
{
    // TODO: Provide lights from buffers
    vec3 lightDirection = vec3(-2,1,-4);
    vec3 lightDiffuse = vec3(1.0, 1.0, 1.0);
    vec3 lighting = lightDiffuse * lambert(fsin_normal, lightDirection);

    // TODO: Provide ambiant amount from buffer
    vec3 ambiantLight = vec3(0.6, 0.55, 0.5);

    vec4 diffuseTexture = texture(sampler2D(DiffuseTexture, DiffuseSampler), fsin_texCoords);
    vec4 diffuse = diffuseTexture * DiffuseTint;

    vec3 result = (clamp(ambiantLight + lighting, 0, 1) * diffuse.xyz);       
    fsout_color = vec4(result, 1.0f);
}