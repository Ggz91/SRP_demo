// Upgrade NOTE: replaced 'glstate_matrix_projection' with 'UNITY_MATRIX_P'

Shader "CusRP/CusLitShader"
{
    Properties
    {
        _Col ("Color", Color) = (1, 1, 0, 1)
        _Tex("Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", float) = 0
        [Enum(On, 1, Off, 0)] _ZWrite ("ZWrite", float) = 1 
        [Toggle(_CLIPPING)] _Clipping("Use Clip", float) = 0
        _Clip("Clip", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.8 
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.8
        [Toggle(_ALPHATODIFFUSE)] _AlphaToDiffuse("Apply alpha to diffuse", float) = 1
    }
    SubShader
    {
        //Tags{"RenderType"="CusRP"}
        Tags{"LightMode"="CusRP"}
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            CGPROGRAM
            #include "UnityCG.cginc"
            #include "../CusShaderLib/Lights/LightsCommon.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _ALPHATODIFFUSE
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Col)
                UNITY_DEFINE_INSTANCED_PROP(float, _Clip)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
           
            sampler2D _Tex;
            struct indata
            {
                float4 pos : POSITION;
                float4 uv : TEXCOORD;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD1;
                float3 pos_ws : TEXCOORD2;
                float3 normal_ws : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(indata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.pos = UnityObjectToClipPos(i.pos);
                o.uv = i.uv;
                o.pos_ws = UnityObjectToWorldDir(i.pos);
                //没有法线贴图在vs里面计算normal_ws
                o.normal_ws = UnityObjectToWorldNormal(i.normal);
                return o;
            }
            float4 frag(v2f indata) : SV_TARGET
            {
                float4 tex_col = tex2D(_Tex, indata.uv.xy);
                #if defined(_CLIPPING)
                    clip(tex_col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Clip));
                #endif
                UNITY_SETUP_INSTANCE_ID(indata);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Col) * tex_col;
                //加入光照影响
                Surface surface;
                #if defined(_ALPHATODIFFUSE)
                    surface.diffuse_use_alpha = true;
                #else
                    surface.diffuse_use_alpha = false;
                #endif
                surface.color = color;
                surface.normal_ws = normalize( indata.normal_ws);
                surface.view_dir = normalize(_WorldSpaceCameraPos -indata.pos_ws) ;
                surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
                surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
                color.xyz = GetLightsColor(surface).xyz;
                return color;
            }
            ENDCG
        }
        Pass {
			Tags { "LightMode" = "ShadowCaster"}

			ColorMask 0

			CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			 UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Col)
                UNITY_DEFINE_INSTANCED_PROP(float, _Clip)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
           
            sampler2D _Tex;
            struct indata
            {
                float4 pos : POSITION;
                float4 uv : TEXCOORD;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD1;
                float3 pos_ws : TEXCOORD2;
                float3 normal_ws : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(indata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.pos = UnityObjectToClipPos(i.pos);
                o.uv = i.uv;
                o.pos_ws = UnityObjectToWorldDir(i.pos);
                //没有法线贴图在vs里面计算normal_ws
                o.normal_ws = UnityObjectToWorldNormal(i.normal);
                return o;
            }
            void frag(v2f indata) 
            {
                float4 tex_col = tex2D(_Tex, indata.uv.xy);
                #if defined(_CLIPPING)
                    clip(tex_col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Clip));
                #endif
            }

			ENDCG
		}
    }


}
