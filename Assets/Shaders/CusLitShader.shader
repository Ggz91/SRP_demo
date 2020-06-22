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
        [Toggle(_SHADOW_CLIP)] _ShadowClip("Shadow Clip", float) = 1
        [Toggle(_RECEIVE_SHADOW)] _RecevieShadow("Recevie Shadow", float) = 1
    }
    SubShader
    {
        //Tags{"RenderType"="CusRP"}
        Pass
        {
            Tags{"LightMode"="CusRP"}
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Off
            HLSLPROGRAM
            #include "../CusShaderLib/Common.hlsl"

            #include "../CusShaderLib/Lights/LightsCommon.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _ALPHATODIFFUSE
            #pragma shader_feature _USE_CASCADE_SHADOW
            #pragma multi_compile _ _PCF3x3 _PCF5x5 _PCF7x7
            #pragma shader_feature _CASCADE_DITHER
            #pragma shader_feature _RECEIVE_SHADOW
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Col)
                UNITY_DEFINE_INSTANCED_PROP(float, _Clip)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
           
            sampler2D _Tex;           

            struct a2v
            {
                float4 pos : POSITION;
                float4 uv : TEXCOORD;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                GI_IN_DATA(1)
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD1;
                float3 pos_ws : TEXCOORD2;
                float3 normal_ws : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                GI_OUT_DATA
            };

            v2f vert(a2v i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.uv = i.uv;
                o.pos_ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(o.pos_ws.xyz);
               
                //没有法线贴图在vs里面计算normal_ws
                o.normal_ws = TransformObjectToWorldNormal(i.normal.xyz);
                TRANSFORM_GI_DATA(i,o)
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
                surface.pos_ws = indata.pos_ws;
                surface.color = color;
                surface.normal_ws = normalize( indata.normal_ws);
                surface.view_dir = normalize(_WorldSpaceCameraPos -indata.pos_ws);
                surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
                surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
                surface.pos = indata.pos.xyz;
                COPY_GI_DATA(indata, surface)
                color.xyz = GetLightsColor(surface).xyz;

                return color;
            }
            ENDHLSL
        }
        Pass 
        {
			Tags { "LightMode" = "ShadowCaster"}
            Cull Off
			//ColorMask 0

			HLSLPROGRAM
            #include "../CusShaderLib/Common.hlsl"

            #pragma vertex vert
            #pragma fragment frag
			#pragma shader_feature _SHADOW_CLIP
			#pragma multi_compile_instancing
            
			 UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Col)
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

                o.uv = i.uv;
                o.pos_ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(o.pos_ws.xyz);
                 #if UNITY_REVERSED_Z
                    o.pos.z = min(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    o.pos.z = max(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                //没有法线贴图在vs里面计算normal_ws
                o.normal_ws = TransformObjectToWorldNormal(i.normal.xyz);
                return o;
            }
            void frag(v2f indata)
            {
                float4 tex_col = tex2D(_Tex, indata.uv.xy);
                #if defined(_SHADOW_CLIP)
                    clip(tex_col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Clip));
                #endif
            }

			ENDHLSL
		}
        Pass
        {
            Tags {"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaVertex
            #pragma fragment MetaFrag
            #include "../CusShaderLib/Common.hlsl"
            #include "../CusShaderLib/Lights/LightsCommon.hlsl"
            
            #pragma shader_feature _ALPHATODIFFUSE

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Col)
                UNITY_DEFINE_INSTANCED_PROP(float, _Clip)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
            
            struct a2v
            {
                float3 pos : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmap_uv : TEXCOORD1;
            };
            
            struct v2f
            {
                float4 pos_cs : SV_POSITION;
                float2 uv : TEXCOORD;
            };

            sampler2D _Tex;  
            float4 _Tex_ST;
            float unity_OneOverOutputBoost;
            float unity_MaxOutputValue;

            v2f MetaVertex(a2v input)
            {
                v2f output;
                input.pos.xy = input.lightmap_uv * unity_LightmapST.xy + unity_LightmapST.zw;
                input.pos.z = input.pos.z > 0.0f ? FLT_MIN : 0.0f;
                output.pos_cs = TransformWorldToHClip(TransformObjectToWorld(input.pos));
                output.uv = input.uv * _Tex_ST.xy + _Tex_ST.zw;
                return output;
            }

            float4 MetaFrag(v2f indata) : SV_TARGET
            {
                float4 color = tex2D(_Tex, indata.uv) * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Col);
                 Surface surface;
                #if defined(_ALPHATODIFFUSE)
                    surface.diffuse_use_alpha = true;
                #else
                    surface.diffuse_use_alpha = false;
                #endif
                //surface.pos_ws = indata.pos_ws;
                surface.color = color;
                //surface.normal_ws = normalize( indata.normal_ws);
                //surface.view_dir = normalize(_WorldSpaceCameraPos -indata.pos_ws);
                surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
                surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);

                float4 brdf_diffuse = CalDiffuse(surface);
                float4 brdf_spe = CalSpecular(surface);
                float roughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
                roughness = PerceptualRoughnessToRoughness(roughness); 
                float4 meta = 0.0f;
                if(unity_MetaFragmentControl.x)
                {
                    meta = float4(brdf_diffuse.xyz, 1.0f);
                    meta.rgb += brdf_spe.rgb * roughness * 0.5f;
                    meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
                }
                return meta;
            }
            ENDHLSL
        }
    }


}
