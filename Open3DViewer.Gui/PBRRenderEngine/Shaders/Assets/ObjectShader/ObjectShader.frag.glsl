#version 450

layout(set = 2, binding = 0) uniform SceneInfo
{
    vec3 AmbientLightColor;
    float padding0;

    vec3 DirectionalLightDirection;
    float padding1;

    vec3 DirectionalLightColor;
    float padding2;

    vec3 CameraPosition;
    float padding3;

    uint ShadingMode;
    uint padding4;
    uint padding5;
    uint padding6;
};

layout(set = 2, binding = 1) uniform MaterialInfo
{
    vec4 DiffuseTint;
};

layout(set = 3, binding = 0) uniform sampler DiffuseSampler;
layout(set = 3, binding = 1) uniform texture2D DiffuseTexture;

layout(set = 4, binding = 0) uniform sampler NormalSampler;
layout(set = 4, binding = 1) uniform texture2D NormalTexture;

layout(location=0) in Vertex
{
	vec3 position;
	vec3 normal;
	vec3 tangent;
	vec3 bitangent;
	vec2 texcoord;
	mat3 tangentBasis;
} vin;

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
    vec3 lighting = clamp(AmbientLightColor + DirectionalLightColor * lambert(vin.normal, DirectionalLightDirection), 0, 10);
    
    // Diffuse color
    vec4 diffuseTexture = texture(sampler2D(DiffuseTexture, DiffuseSampler), vin.texcoord);
    vec4 diffuse = diffuseTexture * DiffuseTint;

    // Specular color
    float specPower = 15.0f;
    float specIntensity = 0.5f;

    vec3 specColor = vec3(0, 0, 0);
    vec3 vertexToEye = normalize(vin.position - CameraPosition);
    vec3 lightReflect = normalize(reflect(DirectionalLightDirection, vin.normal));
    float specularFactor = dot(vertexToEye, lightReflect);
    if (specularFactor > 0)
    {
        specularFactor = pow(abs(specularFactor), specPower);
        specColor = DirectionalLightColor * specIntensity * specularFactor;
    }

    // Extract the normal from the normal map  
    vec4 normalTexture = texture(sampler2D(NormalTexture, NormalSampler), vin.texcoord);
    vec3 normal = normalize(normalTexture.rgb * 2.0 - 1.0);
    float normalDiffuse = max(dot(normal, -DirectionalLightDirection), 0.0);
    vec3 bumpedDiffuse = normalDiffuse * diffuse.rgb;
    
    // Final result
    vec3 final = (lighting * bumpedDiffuse) + specColor;
    vec3 result = final;
    
    if (ShadingMode == 1)
    {
        // [Debug] Draw Diffuse Map Only
        result = (final - final) + diffuseTexture.rgb;
    }
    else if (ShadingMode == 2)
    {
        // [Debug] Draw Normal Map Only
        result = (final - final) + normalTexture.rgb;
    }
    else if (ShadingMode == 3)
    {
        // [Debug] Lighting only
        result = (final - final) + lighting;
    }
    else if (ShadingMode == 4)
    {
        // [Debug] Specular only
        result = (final - final) + specColor;
    }
    else if (ShadingMode == 5)
    {
        // [Debug] Vertex Normal
        result = (final - final) + ((vin.normal + 1.0) * 0.5);
    }
    else if (ShadingMode == 6)
    {
        // [Debug] Vertex Tangent
        result = (final - final) + ((vin.tangent + 1.0) * 0.5);
    }
    else if (ShadingMode == 7)
    {
        // [Debug] Vertex BiTangent
        result = (final - final) + ((vin.bitangent + 1.0) * 0.5);
    }
    else if (ShadingMode == 8)
    {
        // [Debug] Vertex TexCoords
        result = (final - final) + vec3(vin.texcoord, 0);
    }

    fsout_color = vec4(result, 1);
}