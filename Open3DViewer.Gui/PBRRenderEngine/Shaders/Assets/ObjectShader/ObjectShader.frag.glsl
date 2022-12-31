#version 450

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
    // TODO: Provide lights from buffers
    vec3 lightDirection = normalize(vec3(-2,1,-4));
    vec3 lightDiffuse = vec3(1.0, 1.0, 1.0);
    vec3 lighting = lightDiffuse * lambert(fsin_normal, lightDirection);

    // TODO: Provide ambiant amount from buffer
    vec3 ambiantLight = vec3(0.8, 0.75, 0.7);

    // Diffuse color
    vec4 diffuseTexture = texture(sampler2D(DiffuseTexture, DiffuseSampler), fsin_texCoords);
    vec4 diffuse = diffuseTexture * DiffuseTint;

    // Extract the normal from the normal map  
    vec4 normalTexture = texture(sampler2D(NormalTexture, NormalSampler), fsin_texCoords);
    vec3 normal = normalize(normalTexture.rgb * 2.0 - 1.0);
    float normalDiffuse = max(dot(normal, -lightDirection), 0.0);
    vec3 bumpedDiffuse = normalDiffuse * diffuse.rgb;
    
    // Final result
    vec3 final = (clamp(ambiantLight + lighting, 0, 10) * bumpedDiffuse);
    vec3 result = final;
    
    // [Debug] Draw Diffuse Map Only
    //result = (final - final) + diffuseTexture.rgb;

    // [Debug] Draw Normal Map Only
    //result = (final - final) + normalTexture.rgb;
    
    fsout_color = vec4(result, 1);
}