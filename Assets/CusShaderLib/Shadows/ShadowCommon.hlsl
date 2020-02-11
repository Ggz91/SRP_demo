﻿#ifndef SHADOW_COMMON_HLSL
#define SHADOW_COMMON_HLSL

#define MAX_SHADOW_LIGHTS_COUNT 4
#define MAX_CASCADE_COUNT 4 

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_PCF3x3)
	#define PCF_SAMPLER_COUNT 4
	#define PCF_SAMPLER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_PCF5x5)
	#define PCF_SAMPLER_COUNT 9
	#define PCF_SAMPLER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_PCF7x7)
	#define PCF_SAMPLER_COUNT 16
	#define PCF_SAMPLER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

TEXTURE2D_SHADOW(_ShadowMapAltas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(CusShadows)
	float4x4 _ShadowLightSpaceTransformMatrics[MAX_SHADOW_LIGHTS_COUNT * MAX_CASCADE_COUNT];
	float4 _ShadowCascadeCullSphereInfo[MAX_SHADOW_LIGHTS_COUNT * MAX_CASCADE_COUNT];
	float _ShadowMaxDistance;
	float4 _ShadowFadeParam;
	float _ShadowNormalBias[MAX_SHADOW_LIGHTS_COUNT];
	float _ShadowStrength[MAX_SHADOW_LIGHTS_COUNT];
	float4 _ShadowCascadeData[MAX_SHADOW_LIGHTS_COUNT * MAX_CASCADE_COUNT];
	float4 _ShadowAltasSize;
CBUFFER_END

struct ShadowParam
{
	float3 pos_ws;
	int cascade_index;
	uint light_index;
	bool is_mul_lights;
	float strength;
	float depth;
	float3 normal_ws;
	int index;
};
float GetShadowStrength(ShadowParam param)
{
	float strength = _ShadowStrength[param.light_index];
	
	float fade = (1 - param.depth * _ShadowFadeParam.x) * _ShadowFadeParam.y;
	//最后边缘的阴影表现
	if((MAX_CASCADE_COUNT - 1) == param.cascade_index)
	{
		fade *= (1 - param.depth * param.depth * _ShadowCascadeData[param.index].x * _ShadowCascadeData[param.index].x) * _ShadowFadeParam.z;
	}
	return strength;
}
float GetSingleShadowAuttenWithoutCascade(ShadowParam param)
{
	float4x4 ls_matrix = _ShadowLightSpaceTransformMatrics[param.light_index];
	float4 pos_ls = mul(ls_matrix, float4(param.pos_ws + param.normal_ws * _ShadowNormalBias[param.light_index] * _ShadowCascadeData[param.index].y, 1));
	float strength = GetShadowStrength(param);
	return lerp(1.0f, SAMPLE_TEXTURE2D_SHADOW(_ShadowMapAltas, SHADOW_SAMPLER, pos_ls), strength);
}

float SquareDistance(float3 orig, float3 dst)
{
	float3 dst_vector = orig - dst;
	return dot(dst_vector, dst_vector);
}

int GetCascadeIndex(ShadowParam param)
{
	//因为Cascade Shadow Split Data 和Matrics的排列顺序是一样的，因此，这里在确定在哪个Cascade之后就可以得到最终的matrix index
	uint cascade_tile_size = 2;
	uint tile_count = param.is_mul_lights ? 2 : 1;
	int size = param.is_mul_lights ? 2 : 1;
	
	size *= cascade_tile_size;
	int i =0;
	int index = 0;
	for(; i<MAX_CASCADE_COUNT; ++i)
	{
		float2 offset = float2(param.light_index % tile_count, param.light_index / tile_count);
		offset *= cascade_tile_size;
		offset.x += i % cascade_tile_size;
		offset.y += i / cascade_tile_size;
		index = offset.y * size + offset.x;
		float4 split_data = _ShadowCascadeCullSphereInfo[index];
		float distance_to_sphere_center = SquareDistance(param.pos_ws.xyz, split_data.xyz);
		if(distance_to_sphere_center < split_data.w)
		{
			//在当前的Cascade Shadow中
			break;
		}
	}
	
	return index;
}

float GetSingleShadowAuttenWithCascade(ShadowParam param)
{
	if(_ShadowStrength[param.light_index] <= 0)
	{
		return 1.0f;
	}
	//判断是否在剔除距离内
	if(param.depth > _ShadowMaxDistance)
	{
		return 1.0f;
	}
	param.index = GetCascadeIndex(param);
	float4x4 ls_matrix = _ShadowLightSpaceTransformMatrics[param.index];
	float4 pos_ls = mul(ls_matrix, float4(param.pos_ws + param.normal_ws * _ShadowNormalBias[param.light_index], 1));
	float strength = GetShadowStrength(param);
	float shadow = 0;
	#if defined(PCF_SAMPLER_SETUP)
		float weights[PCF_SAMPLER_COUNT];
		float2 poses[PCF_SAMPLER_COUNT];
		float4 size = _ShadowAltasSize.yyxx;
		PCF_SAMPLER_SETUP(size, pos_ls.xy, weights, poses);
		for(int i = 0; i < PCF_SAMPLER_COUNT; ++i)
		{
			shadow += weights[i] * SAMPLE_TEXTURE2D_SHADOW(_ShadowMapAltas, SHADOW_SAMPLER, float3(poses[i].xy, pos_ls.z));
		}
	#else
		shadow = SAMPLE_TEXTURE2D_SHADOW(_ShadowMapAltas, SHADOW_SAMPLER, pos_ls);
	#endif
	return lerp(1.0f, shadow, strength);
}

float GetSingleShadowAutten(ShadowParam param)
{
	#if defined(_USE_CASCADE_SHADOW)
		return	GetSingleShadowAuttenWithCascade(param);
	#else
		return GetSingleShadowAuttenWithoutCascade(param);
	#endif
}

/*float GetShadowAutten(ShadowParam param)
{
	float att = 1;
	for(int i = 0; i < _ShadowLightsCount; ++i)
	{
		att *= GetSingleShadowAutten(i, param);
	}
	return att;
}*/

#endif