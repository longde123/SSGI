#version 450

in vec2 TexCoord;

layout (location = 0) out vec4 outColor;
//layout (location = 1) out vec4 outReflection;

layout (std140, binding = 9) uniform MatCam
{
    mat4 projection;
	mat4 projectionInverse;
    mat4 view;
	mat4 viewInverse;
	mat4 kinectProjection;
	mat4 kinectProjectionInverse;
};

//uniform vec2 bufferSize = vec2(1920, 1080);
//uniform vec2 texelSize = 1.0 / vec2(1920, 1080);

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gColor;
uniform sampler2D aoMap;
uniform samplerCube irradianceMap;
uniform samplerCube prefilterMap;
uniform sampler2D brdfLUT;
uniform sampler2D reflectionMap;

// PBR variables
uniform vec3 cameraPosition;
uniform vec3 lightPosition;
uniform vec3 lightColor;
uniform float metallic;
uniform float roughness;
const float pi = 3.1415926;



// Helper functions

vec3 depthToViewPosition(float depth, vec2 texcoord)
{
    vec4 clipPosition = vec4(texcoord * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    vec4 viewPosition = projectionInverse * clipPosition;
    return viewPosition.xyz / viewPosition.w;
}

// Rendering equation, cook torrance brdf

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = pi * denom * denom;

    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}   

vec3 render(vec3 P, vec3 N, vec4 inColor, vec4 inReflection, vec3 ao, out vec3 outEnvColor)
{
	vec3 albedo = inColor.rgb;
	float mRoughness = roughness;
	float mMetallic = metallic;
	
	if (inColor.a == 0)
	{
		mRoughness = 0.55;
	}
	
	
	vec3 V = normalize(cameraPosition - P);
	//vec3 V = normalize(-P);
	vec3 R = reflect(-V, N); 
	
	// calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
    // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)
    vec3 F0 = mix(vec3(0.04), albedo, mMetallic);
	
	// reflectance equation
    vec3 Lo = vec3(0.0);
    for (int i = 0; i < 1; ++i) 
    {
        // calculate per-light radiance
        vec3 L = normalize(lightPosition - P);
        vec3 H = normalize(V + L);
        float distance = length(lightPosition - P);
        float attenuation = 1.0 / (distance * distance);
        vec3 radiance = lightColor * attenuation;

        // Cook-Torrance BRDF
        float NDF = DistributionGGX(N, H, mRoughness);   
        float G   = GeometrySmith(N, V, L, mRoughness);    
        vec3 F    = fresnelSchlick(max(dot(H, V), 0.0), F0);        
        
        vec3 nominator    = NDF * G * F;
        float denominator = 4 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.001; // 0.001 to prevent divide by zero.
        vec3 specular = nominator / denominator;
        
         // kS is equal to Fresnel
        vec3 kS = F;
        // for energy conservation, the diffuse and specular light can't
        // be above 1.0 (unless the surface emits light); to preserve this
        // relationship the diffuse component (kD) should equal 1.0 - kS.
        vec3 kD = 1.0 - kS;
        // multiply kD by the inverse metalness such that only non-metals 
        // have diffuse lighting, or a linear blend if partly metal (pure metals
        // have no diffuse light).
        kD *= 1.0 - mMetallic;	                
            
        // scale light by NdotL
        float NdotL = max(dot(N, L), 0.0);        

        // add to outgoing radiance Lo
        Lo += (kD * albedo / pi + specular) * radiance * NdotL; // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
    }
	
	// ambient lighting (we now use IBL as the ambient term)
    vec3 F = fresnelSchlickRoughness(max(dot(N, V), 0.0), F0, mRoughness);
    
    vec3 kS = F;
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - mMetallic;
	
	vec3 irradiance = texture(irradianceMap, N).rgb;
	vec3 diffuse = irradiance * albedo;
	
	// sample both the pre-filter map and the BRDF lut and combine them together as per the Split-Sum approximation to get the IBL specular part.
    const float MAX_REFLECTION_LOD = 5.0;
    vec3 prefilteredColor = textureLod(prefilterMap, R, mRoughness * MAX_REFLECTION_LOD).rgb;
	vec4 reflectionColor = inReflection;
	//reflectionColor.rgb += reflectionColor.rgb;
	//reflectionColor.rgb = mix(reflectionColor.rgb, reflectionColor.rgb + prefilteredColor, 1 - pow(1 - metallic, 5));
	//reflectionColor.rgb = mix(reflectionColor.rgb, prefilteredColor, roughness);
	//reflectionColor.rgb = mix(reflectionColor.rgb, reflectionColor.rgb * irradiance, 1 - pow(1 - metallic, 5));
	//reflectionColor.a *= 1 - pow(metallic, 8);
	vec3 envColor = mix(prefilteredColor, reflectionColor.rgb, reflectionColor.a);
	//envColor = prefilteredColor;
	//envColor = reflectionColor.rgb;
	
	//vec3 dscolor = texture(dsColor, TexCoord).rgb;
	//envColor = mix(envColor, dscolor, 1-inColor.a);
	outEnvColor = envColor;
	
    vec2 brdf = texture(brdfLUT, vec2(max(dot(N, V), 0.0), mRoughness)).rg;
    vec3 specular = envColor * (F * brdf.x + brdf.y);

    vec3 ambient = (kD * diffuse + specular) * ao;
    vec3 color = ambient + Lo;
	
	
	return color;
}


// Main

void main()
{
	//float depth = texture(gDepth, TexCoord).r;
	vec3 position = texture(gPosition, TexCoord).xyz;
	vec3 positionWorld = (viewInverse * vec4(position, 1)).xyz;
    vec3 normal = texture(gNormal, TexCoord).xyz;
	vec3 normalWorld = mat3(viewInverse) * normal;
    vec4 color = texture(gColor, TexCoord);
	vec4 reflection = texture(reflectionMap, TexCoord);
	
	// Reconstruct position from depth buffer
	//position = depthToViewPosition(depth, TexCoord);
	
	
	vec4 finalColor = vec4(0);
	vec3 ao = texture(aoMap, TexCoord).rgb;
	vec3 envColor;
	finalColor.rgb = render(positionWorld, normalWorld, color, reflection, ao, envColor);
	

	outColor = finalColor;
	outColor.a = color.a;
	
	//outReflection = reflection;
}