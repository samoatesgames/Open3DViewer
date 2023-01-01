#version 450

layout(set = 2, binding = 0) uniform MaterialInfo
{
    vec4 DiffuseTint;

    vec3 AmbientLightColor;
    float padding0;

    vec3 DirectionalLightDirection;
    float padding1;

    vec3 DirectionalLightColor;
    float padding2;
};

layout(set = 3, binding = 0) uniform sampler DiffuseSampler;
layout(set = 3, binding = 1) uniform texture2D DiffuseTexture;

layout(set = 4, binding = 0) uniform sampler NormalSampler;
layout(set = 4, binding = 1) uniform texture2D NormalTexture;

layout(location = 0) in vec3 fsin_normal;
layout(location = 1) in vec2 fsin_texCoords;

layout(location = 0) out vec4 fsout_color;

float lambert(vec3 N, vec3 L)
{
  vec3 nrmN = normalize(N);
  vec3 nrmL = normalize(L);
  float result = dot(nrmN, nrmL);
  return max(result, 0.0);
}

void main()
{
    // Calculate our basic lambert lighting
    vec3 lighting = DirectionalLightColor * lambert(fsin_normal, DirectionalLightDirection.xyz);

    // Diffuse color
    vec4 diffuseTexture = texture(sampler2D(DiffuseTexture, DiffuseSampler), fsin_texCoords);
    vec4 diffuse = diffuseTexture * DiffuseTint;

    // Extract the normal from the normal map  
    vec4 normalTexture = texture(sampler2D(NormalTexture, NormalSampler), fsin_texCoords);
    vec3 normal = normalize(normalTexture.rgb * 2.0 - 1.0);
    float normalDiffuse = max(dot(normal, -DirectionalLightDirection), 0.0);
    vec3 bumpedDiffuse = normalDiffuse * diffuse.rgb;
    
    // Final result
    vec3 final = (clamp(AmbientLightColor + lighting, 0, 10) * bumpedDiffuse);
    vec3 result = final;
    
    // [Debug] Draw Diffuse Map Only
    //result = (final - final) + diffuseTexture.rgb;

    // [Debug] Draw Normal Map Only
    //result = (final - final) + normalTexture.rgb;
    
    fsout_color = vec4(result, 1);
}