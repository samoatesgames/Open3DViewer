#version 450

// PBR implementation based upon: https://github.com/Nadrin/PBR

const float PI = 3.141592;
const float Epsilon = 0.00001;
const int NumLights = 3;
const vec3 Fdielectric = vec3(0.04);

const uint IsDiffuseTextureBound = 1;
const uint IsNormalTextureBound = 2;
const uint IsMetallicRoughnessTextureBound = 4;
const uint IsEmissiveTextureBound = 8;
const uint IsOcclusionTextureBound = 16;

const uint AlphaMode_Opaque = 0;
const uint AlphaMode_Mask = 1;
const uint AlphaMode_Blend = 2;

struct DirectionalLight
{
	vec3 direction;
	uint isActive;

	vec3 radiance;
	float padding_radiance;
};

layout(set = 2, binding = 0) uniform SceneInfo
{
	DirectionalLight Lights[NumLights];

    vec3 AmbientLightColor;
    float padding0;

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
    vec2 MetallicRoughnessFactor;
	uint BoundTextureBitMask;
	float OcclusionStrength;
	vec4 EmissiveFactors;
	uint AlphaMode;
	float AlphaCutOff;

	vec2 materialInfoPadding0;
};

layout(set = 3, binding = 0) uniform sampler DiffuseSampler;
layout(set = 3, binding = 1) uniform texture2D DiffuseTexture;

layout(set = 4, binding = 0) uniform sampler NormalSampler;
layout(set = 4, binding = 1) uniform texture2D NormalTexture;

layout(set = 5, binding = 0) uniform sampler MetallicRoughnessSampler;
layout(set = 5, binding = 1) uniform texture2D MetallicRoughnessTexture;

layout(set = 6, binding = 0) uniform sampler EmissiveSampler;
layout(set = 6, binding = 1) uniform texture2D EmissiveTexture;

layout(set = 7, binding = 0) uniform sampler OcclusionSampler;
layout(set = 7, binding = 1) uniform texture2D OcclusionTexture;

layout(location=0) in Vertex
{
	vec3 position;
	vec3 normal;
	vec3 tangent;
	vec3 bitangent;
	vec2 texcoord;
	vec4 color;
	mat3 tangentBasis;
} vin;

layout(location = 0) out vec4 fsout_color;

// GGX/Towbridge-Reitz normal distribution function.
// Uses Disney's reparametrization of alpha = roughness^2.
float ndfGGX(float cosLh, float roughness)
{
	float alpha   = roughness * roughness;
	float alphaSq = alpha * alpha;

	float denom = (cosLh * cosLh) * (alphaSq - 1.0) + 1.0;
	return alphaSq / (PI * denom * denom);
}

// Single term for separable Schlick-GGX below.
float gaSchlickG1(float cosTheta, float k)
{
	return cosTheta / (cosTheta * (1.0 - k) + k);
}

// Schlick-GGX approximation of geometric attenuation function using Smith's method.
float gaSchlickGGX(float cosLi, float cosLo, float roughness)
{
	float r = roughness + 1.0;
	float k = (r * r) / 8.0; // Epic suggests using this roughness remapping for analytic lights.
	return gaSchlickG1(cosLi, k) * gaSchlickG1(cosLo, k);
}

// Shlick's approximation of the Fresnel factor.
vec3 fresnelSchlick(vec3 F0, float cosTheta)
{
	return F0 + (vec3(1.0) - F0) * pow(1.0 - cosTheta, 5.0);
}

void main()
{
    // Sample input textures to get shading model params.
	vec4 albedo = texture(sampler2D(DiffuseTexture, DiffuseSampler), vin.texcoord);
	albedo *= DiffuseTint;
	albedo *= vin.color;

	if (AlphaMode == AlphaMode_Opaque)
	{
		albedo.a = 1.0f;
	}
	else if (AlphaMode == AlphaMode_Mask)
	{
		if (albedo.a < AlphaCutOff)
		{
			discard;
		}
	}

	// Get the metalic/roughness values
	float metalness = 1.0f;
	float roughness = 1.0f;
	if ((BoundTextureBitMask & IsMetallicRoughnessTextureBound) == IsMetallicRoughnessTextureBound)
	{
		vec3 metalicRoughtness = texture(sampler2D(MetallicRoughnessTexture, MetallicRoughnessSampler), vin.texcoord).rgb;
		metalness = metalicRoughtness.b;
		roughness = metalicRoughtness.g;
	}
	metalness *= MetallicRoughnessFactor.r;
	roughness *= MetallicRoughnessFactor.g;

	// Emmisive values
	vec3 emissive = EmissiveFactors.xyz;
	if ((BoundTextureBitMask & IsEmissiveTextureBound) == IsEmissiveTextureBound)
	{
		emissive *= texture(sampler2D(EmissiveTexture, EmissiveSampler), vin.texcoord).rgb;
	}
	emissive *= EmissiveFactors.w;

	// Occlusion values
	float occlision = 1;
	if ((BoundTextureBitMask & IsOcclusionTextureBound) == IsOcclusionTextureBound)
	{
		occlision = texture(sampler2D(OcclusionTexture, OcclusionSampler), vin.texcoord).r;
	}
	occlision *= OcclusionStrength;

    // Outgoing light direction (vector from world-space fragment position to the "eye").
	vec3 Lo = normalize(CameraPosition - vin.position);

    // Get current fragment's normal and transform to world space.
    vec3 normalTexture = texture(sampler2D(NormalTexture, NormalSampler), vin.texcoord).rgb;
	vec3 N = normalize(2.0 * normalTexture - 1.0);
	N = normalize(vin.tangentBasis * N);

    // Angle between surface normal and outgoing light direction.
	float cosLo = max(0.0, dot(N, Lo));
		
	// Specular reflection vector.
	vec3 Lr = 2.0 * cosLo * N - Lo;

	// Fresnel reflectance at normal incidence (for metals use albedo color).
	vec3 F0 = mix(Fdielectric, albedo.rgb, metalness);

    // Direct lighting calculation for analytical lights.
	vec3 directLighting = vec3(0);
	for(int i=0; i<NumLights; ++i)
	{
		if (Lights[i].isActive == 0)
		{
			continue;
		}

		vec3 Li = Lights[i].direction;
		vec3 Lradiance = Lights[i].radiance;

		// Half-vector between Li and Lo.
		vec3 Lh = normalize(Li + Lo);

		// Calculate angles between surface normal and various light vectors.
		float cosLi = max(0.0, dot(N, Li));
		float cosLh = max(0.0, dot(N, Lh));

		// Calculate Fresnel term for direct lighting. 
		vec3 F  = fresnelSchlick(F0, max(0.0, dot(Lh, Lo)));
		// Calculate normal distribution for specular BRDF.
		float D = ndfGGX(cosLh, roughness);
		// Calculate geometric attenuation for specular BRDF.
		float G = gaSchlickGGX(cosLi, cosLo, roughness);

		// Diffuse scattering happens due to light being refracted multiple times by a dielectric medium.
		// Metals on the other hand either reflect or absorb energy, so diffuse contribution is always zero.
		// To be energy conserving we must scale diffuse BRDF contribution based on Fresnel factor & metalness.
		vec3 kd = mix(vec3(1.0) - F, vec3(0.0), metalness);

		// Lambert diffuse BRDF.
		// We don't scale by 1/PI for lighting & material units to be more convenient.
		// See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
		vec3 diffuseBRDF = kd * albedo.rgb;

		// Cook-Torrance specular microfacet BRDF.
		vec3 specularBRDF = (F * D * G) / max(Epsilon, 4.0f * cosLi * cosLo);

		// Total contribution for this light.
		directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi;
	}

	// Ambient lighting (IBL).
	vec3 ambientLighting;
	{
		// Sample diffuse irradiance at normal direction.
		vec3 irradiance = AmbientLightColor;

		// Calculate Fresnel term for ambient lighting.
		// Since we use pre-filtered cubemap(s) and irradiance is coming from many directions
		// use cosLo instead of angle with light's half-vector (cosLh above).
		// See: https://seblagarde.wordpress.com/2011/08/17/hello-world/
		vec3 F = fresnelSchlick(F0, cosLo);

		// Get diffuse contribution factor (as with direct lighting).
		vec3 kd = mix(vec3(1.0) - F, vec3(0.0), metalness);

		// Irradiance map contains exitant radiance assuming Lambertian BRDF, no need to scale by 1/PI here either.
		vec3 diffuseIBL = kd * albedo.rgb * irradiance;

		vec3 specularIrradiance = vec3(1.0f, 1.0f, 1.0f);

		// Split-sum approximation factors for Cook-Torrance specular BRDF.
		vec2 specularBRDF = vec2(0.6f, 0.05f);

		// Total specular IBL contribution.
		vec3 specularIBL = (F0 * specularBRDF.x + specularBRDF.y) * specularIrradiance;

		// Total ambient lighting contribution.
		ambientLighting = diffuseIBL + specularIBL;
	}

    // Final lighting
    vec3 final = ((directLighting + ambientLighting) * occlision) + emissive;

    // Store our final output result, but we may overwrite it below.
    // This is to avoid shader optimization removing things that are "never" used.
    vec3 result = final;
    
    if (ShadingMode == 1)
    {
        // [Debug] Draw Diffuse Map Only
        result = (final - final) + albedo.rgb;
    }
    else if (ShadingMode == 2)
    {
        // [Debug] Draw Normal Map Only
        result = (final - final) + normalTexture;
    }
	else if (ShadingMode == 3)
    {
        // [Debug] Draw Metallic Map Only
        result = (final - final) + vec3(metalness, metalness, metalness);
    }
	else if (ShadingMode == 4)
    {
        // [Debug] Draw Roughness Map Only
        result = (final - final) + vec3(roughness, roughness, roughness);
    }
	else if (ShadingMode == 5)
    {
        // [Debug] Draw Occlusion Map Only
        result = (final - final) + occlision;
    }
	else if (ShadingMode == 6)
    {
        // [Debug] Draw Emmisive Map Only
        result = (final - final) + emissive;
    }
    else if (ShadingMode == 7)
    {
        // [Debug] Lighting only
        result = (final - final) + directLighting;
    }
    else if (ShadingMode == 8)
    {
        // [Debug] Ambient only
        result = (final - final) + ambientLighting;
    }
    else if (ShadingMode == 9)
    {
        // [Debug] Vertex Normal
        result = (final - final) + ((vin.normal + 1.0) * 0.5);
    }
    else if (ShadingMode == 10)
    {
        // [Debug] Vertex Tangent
        result = (final - final) + ((vin.tangent + 1.0) * 0.5);
    }
    else if (ShadingMode == 11)
    {
        // [Debug] Vertex BiTangent
        result = (final - final) + ((vin.bitangent + 1.0) * 0.5);
    }
    else if (ShadingMode == 12)
    {
        // [Debug] Vertex TexCoords
        result = (final - final) + vec3(vin.texcoord, 0);
    }
	else if (ShadingMode == 13)
    {
        // [Debug] Vertex Position
        result = (final - final) + vin.position;
    }

    fsout_color = vec4(result, albedo.a);
}