#ifndef LIGHTS_COMMON_HLSL
#define LIGHTS_COMMON_HLSL

struct Surface
{
    float4 color;
    float3 normal_ws;
    float smoothness;
    float fresnel;
    float metallic;
    float3 view_dir;
    bool diffuse_use_alpha;
    float3 pos_ws;
    float3 pos;
    #ifdef LIGHTMAP_ON
    float2 lightmap_uv;
    #endif
};
struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
    float perceptual_roughness;
    float fresnel;
};

#include "../CusShaderLib/Shadows/ShadowCommon.hlsl"
#include "../CusShaderLib/BakedLights/GI.hlsl"

#define MAX_LIGHTS_NUM 4
#define MIN_REFLECTIVITY 0.04
CBUFFER_START(Lights_Prop)
    int _LightsCount;
    float4 _LightsDirections[MAX_LIGHTS_NUM];
    float4 _LightsColors[MAX_LIGHTS_NUM];
CBUFFER_END




float4 CalDiffuse(Surface surface)
{
    float range = 1 - MIN_REFLECTIVITY;
    float alpha = surface.diffuse_use_alpha ? surface.color.a : 1;
    return surface.color * range * (1 - surface.metallic) * alpha;
}

float4 CalSpecular(Surface surface)
{
    return lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
}
BRDF GetBRDF(Surface surface)
{
    BRDF brdf = (BRDF)0;
    brdf.perceptual_roughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptual_roughness);
    brdf.diffuse = CalDiffuse(surface).xyz;
    brdf.specular = CalSpecular(surface).xyz;
    brdf.fresnel = saturate(surface.smoothness + 1.0 - ((1 - MIN_REFLECTIVITY) - surface.metallic*(1 - MIN_REFLECTIVITY)));
    return brdf;
}
float Square(float v)
{
    return v * v;
}

float CalSpecularStrength(Surface surface, float4 light_dir, float roughness)
{
    float3 h = normalize(light_dir.xyz + surface.view_dir);
    float nh2 = Square(saturate(dot(surface.normal_ws, h)));
    float lh2 = Square(saturate(dot(light_dir.xyz, h)));
    float r2 = Square(roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float fresnel_strength = surface.fresnel * Pow4(1.0 - saturate(dot(surface.normal_ws, surface.view_dir)));
	float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnel_strength);
	reflection /= brdf.roughness * brdf.roughness + 1.0f;
	return diffuse * brdf.diffuse + reflection;
}

float4 GetBRDF(float4 light_dir, float4 color, Surface surface, BRDF brdf)
{
    float4 diffuse = float4(brdf.diffuse, 1);
    float4 specular = float4(brdf.specular, 1);
    float4 specular_strength = CalSpecularStrength(surface, light_dir, brdf.roughness);
    float4 brdf_col = diffuse + specular * specular_strength;
    return brdf_col;
}

float4 GetSingleLightsColor(int index, Surface surface, GI gi, BRDF brdf)
{
    
    float4 dir = -_LightsDirections[index];
    float4 color = _LightsColors[index];
    color.rgb += gi.diffuse * CalDiffuse(surface).rgb;
    //return float4(gi.shadow_mask.shadows.rgb, 1);
    float4 light_color = saturate(dot(surface.normal_ws, dir.xyz))*color;
    return light_color * GetBRDF(dir, color, surface, brdf);
}

float4 GetLightsColor(Surface surface)
{
    BRDF brdf = GetBRDF(surface);
    //加入阴影的影响
    ShadowParam shadow = (ShadowParam)0;
    //加入normal bias
    shadow.is_mul_lights = _LightsCount > 1;
	shadow.depth = -TransformWorldToView(surface.pos_ws.xyz).z;
    shadow.pos_ws = surface.pos_ws;
    shadow.normal_ws = surface.normal_ws;
    shadow.index = 0;
    
    //加入lightMap的影响
    GI gi = GetGI(surface, brdf);
    shadow.shadow_mask = gi.shadow_mask;
    
    #if defined(_CASCADE_DITHER)
    shadow.dither = InterleavedGradientNoise(surface.pos.xy, 0);
    #endif
    float4 color = 1;
    color.xyz = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for(int i=0; i<_LightsCount; ++i)
    {
        shadow.light_index = i;
        shadow.shadow_mask_channel = _ShadowMaskChannel[i];
        color += GetSingleLightsColor(i, surface, gi, brdf) * GetSingleShadowAutten(shadow);
    }

   
    return color;
}

#endif