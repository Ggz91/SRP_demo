#ifndef SHADOW_COMMON_HLSL
#define SHADOW_COMMON_HLSL
#define MAX_SHADOW_LIGHTS_COUNT 4

TEXTURE2D_SHADOW(_ShadowMapAltas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(CusShadows)
	float4x4 _ShadowLightSpaceTransformMatrics[MAX_SHADOW_LIGHTS_COUNT];
CBUFFER_END

struct ShadowParam
{
	float4 pos_ws;
};

float GetSingleShadowAutten(int i, ShadowParam param)
{
	float4x4 ls_matrix = _ShadowLightSpaceTransformMatrics[i];
	float4 pos_ls = mul(ls_matrix, param.pos_ws);
	return SAMPLE_TEXTURE2D_SHADOW(
		_ShadowMapAltas, SHADOW_SAMPLER, pos_ls);
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