#ifndef SHADOW_COMMON_HLSL
#define SHADOW_COMMON_HLSL

#define MAX_SHADOW_LIGHTS_COUNT 4
#define MAX_CASCADE_COUNT 4 


TEXTURE2D_SHADOW(_ShadowMapAltas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(CusShadows)
	float4x4 _ShadowLightSpaceTransformMatrics[MAX_SHADOW_LIGHTS_COUNT * MAX_CASCADE_COUNT];
	float4 _ShadowCascadeCullSphereInfo[MAX_SHADOW_LIGHTS_COUNT * MAX_CASCADE_COUNT];
	float _ShadowMaxDistance;
CBUFFER_END

struct ShadowParam
{
	float4 pos_ws;
	float4 cascade_index;
	uint light_index;
	bool is_mul_lights;
	float strength;
	float depth;
};

float GetSingleShadowAuttenWithoutCascade(ShadowParam param)
{
	float4x4 ls_matrix = _ShadowLightSpaceTransformMatrics[param.light_index];
	float4 pos_ls = mul(ls_matrix, param.pos_ws);
	return SAMPLE_TEXTURE2D_SHADOW(
		_ShadowMapAltas, SHADOW_SAMPLER, pos_ls);
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
	float2 offset = float2(param.light_index % tile_count, param.light_index / tile_count);
	offset *= cascade_tile_size;
	int i =0;
	int index = 0;
	for(; i<MAX_CASCADE_COUNT; ++i)
	{
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
float GetShadowStrength(ShadowParam param)
{
	float strength = 1;
	//判断是否在剔除距离内
	strength *= step(param.depth, _ShadowMaxDistance);
	return strength;
}
float GetSingleShadowAuttenWithCascade(ShadowParam param)
{
	int index = GetCascadeIndex(param);
	float4x4 ls_matrix = _ShadowLightSpaceTransformMatrics[index];
	float4 pos_ls = mul(ls_matrix, param.pos_ws);
	return SAMPLE_TEXTURE2D_SHADOW(
		_ShadowMapAltas, SHADOW_SAMPLER, pos_ls);
}

float GetSingleShadowAutten(ShadowParam param)
{
	int strength = GetShadowStrength(param);
	#if defined(_USE_CASCADE_SHADOW)
		return	GetSingleShadowAuttenWithCascade(param) * strength;
	#else
		return GetSingleShadowAuttenWithoutCascade(param) * strength;
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