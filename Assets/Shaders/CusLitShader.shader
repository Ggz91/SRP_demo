Shader "CusRP/CusLitShader"
{
    Properties
    {
        _Col ("Color", Color) = (1, 1, 0, 1)
    }
    SubShader
    {
        //Tags{"RenderType"="CusRP"}
        Tags{"LightMode"="CusRP"}
        Pass
        {
            
            CGPROGRAM
            #include "Library\PackageCache\com.unity.render-pipelines.core@7.1.7\ShaderLibrary\UnityInstancing.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing


            #if defined(UNITY_INSTANCING_ENABLED)
                UNITY_INSTANCING_BUFFER_START(Prop)
                    UNITY_DEFINE_INSTANCED_PROP(fixed4, _Col)
                UNITY_INSTANCING_BUFFER_END(Prop)
            #else
                CBUFFER_START(UnityPerMaterial)
                    fixed4 _Col;
                CBUFFER_END
            #endif

            struct indata
            {
                float4 pos : POSITION;
                #if defined(UNITY_INSTANCING_ENABLED)
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                #endif
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                #if defined(UNITY_INSTANCING_ENABLED)
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                #endif
            };

            struct f2a
            {
                float4 pos : POSITION;
                float4 color : Color;
            };

            v2f vert(indata i)
            {
                v2f o;
                #if defined(UNITY_INSTANCING_ENABLED)
                    UNITY_SETUP_INSTANCE_ID(i);
                    UNITY_TRANSFER_INSTANCE_ID(i, o);
                #endif

                o.pos = UnityObjectToClipPos(i.pos);

                return o;
            }
            float4 frag(v2f indata) : SV_TARGET
            {
                #if defined(UNITY_INSTANCING_ENABLED)
                    UNITY_SETUP_INSTANCE_ID(indata);
                    float4 color = UNITY_ACCESS_INSTANCED_PROP(Prop, _Col);
                #else
                    float4 color = _Col;
                #endif
                return color;
            }
            ENDCG
        }
    }
}
