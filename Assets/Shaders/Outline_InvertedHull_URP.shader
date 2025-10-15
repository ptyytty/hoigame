Shader "Custom/Outline_InvertedHull_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,1,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.08
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // ----- PASS 1: 원메시 스텐실 마스크 -----
        Pass
        {
            Name "Mask"
            Tags{ "LightMode"="SRPDefaultUnlit" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float4 posWS = mul(unity_ObjectToWorld, v.positionOS);
                o.positionHCS = mul(unity_MatrixVP, posWS);
                return o;
            }

            float4 frag (Varyings i) : SV_Target { return float4(0,0,0,0); }
            ENDHLSL
        }

        // ----- PASS 2: 인버티드 헐 아웃라인 -----
        Pass
        {
            Name "Outline"
            Tags{ "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            // ZTest LEqual  →  Always 로 완화해 보이게
            ZTest Always

            // 배경 위에 확실히 칠해지게 블렌딩 끔(불투명 색)
            Blend Off

            // 살짝 깊이 오프셋을 줘서 깜빡임 방지
            Offset 1, 1

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;

            float4 _OutlineColor;
            float  _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            float3 TransformNormal(float3 nOS)
            {
                float3x3 m = (float3x3)unity_ObjectToWorld;
                return normalize(mul(m, nOS));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = mul(unity_ObjectToWorld, v.positionOS).xyz;
                float3 nWS   = TransformNormal(v.normalOS);
                posWS += nWS * _OutlineWidth;
                o.positionHCS = mul(unity_MatrixVP, float4(posWS,1));
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;   // 알파 1.0 기준 (Blend Off)
            }
            ENDHLSL
        }
    }

    FallBack Off
}
