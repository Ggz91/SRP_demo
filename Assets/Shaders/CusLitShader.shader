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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Col)
                UNITY_DEFINE_INSTANCED_PROP(float, _Clip)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
           
            sampler2D _Tex;
            struct indata
            {
                float4 pos : POSITION;
                float4 uv : TEXCOORD;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct f2a
            {
                float4 pos : POSITION;
                float4 color : Color;
            };

            v2f vert(indata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.pos = UnityObjectToClipPos(i.pos);
                o.uv = i.uv;
                return o;
            }
            float4 frag(v2f indata) : SV_TARGET
            {
                float4 tex_col = tex2D(_Tex, indata.uv.xy);
                #if defined(_CLIPPING)
                    clip(tex_col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Clip));
                #endif
                UNITY_SETUP_INSTANCE_ID(indata);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Col);
                return color * tex_col;
            }
            ENDCG
        }
    }
}
