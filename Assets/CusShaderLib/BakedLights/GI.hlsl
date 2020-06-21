#ifndef CUSTOM_GI
#define CUSTOM_GI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#ifdef LIGHTMAP_ON
    #define GI_IN_DATA(NO)  float2 lightmap_uv : TEXCOORD##NO;
    #define GI_OUT_DATA float2 lightmap_uv : VAR_LIGHT_MAP_UV;
    #define TRANSFORM_GI_DATA(in, out) out.lightmap_uv = in.lightmap_uv * unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(in) in.lightmap_uv
    #define COPY_GI_DATA(in, out) out.lightmap_uv = in.lightmap_uv;
#else
    #define GI_IN_DATA(NO)
    #define GI_OUT_DATA 
    #define TRANSFORM_GI_DATA(in, out) 
    #define GI_FRAGMENT_DATA(in) 0.0
    #define COPY_GI_DATA(in, out) 
#endif

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

struct GI
{
    float3 diffuse;
};

float3 SampleLightMap(float2 lightmap_uv)
{
    #ifdef LIGHTMAP_ON
        return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightmap_uv, float4(1.0, 1.0, 0.0, 0.0),
        #ifdef UNITY_LIGHTMAP_FULL_HDR
            false,
        #else
            true,
        #endif
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0));
    #else
        return 0.0f;
    #endif
}

GI GetGI(float2 lightmap_uv)
{
    GI gi;
    gi.diffuse = SampleLightMap(lightmap_uv);
    return gi;
}
#endif