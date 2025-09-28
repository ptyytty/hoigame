Shader "Custom/Outline_InvertedHull_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,0.85,0.2,1)
        _OutlineWidth ("Outline Width", Float) = 0.01
    }
    SubShader
    {
        // URP 대상임을 명시
        Tags { 
            "RenderType"="Opaque" 
            "Queue"="Geometry" 
            "RenderPipeline"="UniversalRenderPipeline" 
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Offset 1, 1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float  _OutlineWidth;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 nWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 pWS = TransformObjectToWorld(v.positionOS.xyz);
                pWS += nWS * _OutlineWidth;               // 노멀 방향으로 팽창
                o.positionHCS = TransformWorldToHClip(pWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;                     // 단색 외곽선
            }
            ENDHLSL
        }
    }
    FallBack Off
}
