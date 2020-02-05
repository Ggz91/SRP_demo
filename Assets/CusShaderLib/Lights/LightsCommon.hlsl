﻿#ifndef LIGHTS_COMMON_HLSL
#define LIGHTS_COMMON_HLSL

#define MAX_LIGHTS_NUM 4
#define MIN_REFLECTIVITY 0.04
CBUFFER_START(Lights_Prop)
    int _LightsCount;
    float4 _LightsDirections[MAX_LIGHTS_NUM];
    float4 _LightsColors[MAX_LIGHTS_NUM];
CBUFFER_END

struct Surface
{
    float4 color;
    float3 normal_ws;
    float smoothness;
    float metallic;
    float view_dir;
};
float PerceptualSmoothnessToPerceptualRoughness(float perceptualSmoothness)
{
    return (1.0 - perceptualSmoothness);
}
float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}
float4 CalDiffuse(float4 light_dir, Surface surface)
{
    float range = 1 - MIN_REFLECTIVITY;
    return surface.color * range * (1 - surface.metallic);
}

float4 CalSpecular(Surface surface)
{
    return lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
}
float Square(float v)
{
    return v * v;
}

float CalSpecularStrength(Surface surface, float4 light_dir)
{
    float roughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    roughness = PerceptualRoughnessToRoughness(roughness);
    float3 h = normalize(light_dir + surface.view_dir);
    float nh2 = Square(saturate(dot(surface.normal_ws, h)));
    float lh2 = Square(saturate(dot(light_dir, h)));
    float r2 = Square(roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float4 GetBRDF(float4 light_dir, float4 color, Surface surface)
{
    float4 diffuse = CalDiffuse(light_dir, surface);
    float4 specular = CalSpecular(surface);
    float4 specular_strength = CalSpecularStrength(surface, light_dir);
    float4 brdf_col = diffuse + specular * specular_strength;
    return brdf_col;
}

float4 GetSingleLightsColor(int index, Surface surface)
{
    float4 dir = -_LightsDirections[index];
    float4 color = _LightsColors[index];
    float4 light_color = saturate(dot(surface.normal_ws, dir))*color;
    return light_color * GetBRDF(dir, color, surface);
}

float4 GetLightsColor(Surface surface)
{
    float4 color = 0;
    for(int i=0; i<_LightsCount; ++i)
    {
        color += GetSingleLightsColor(i, surface);
    }
    return color;
}

#endif