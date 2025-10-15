Shader "Custom/Outline_HybridStencilZ_URP"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,1,0,1)
        _OutlineWidth("Outline Width", Float) = 0.08
    }

    SubShader
    {
        Tags { "Queue"="Geometry+1" "RenderType"="Opaque" }

        // PASS 1) 마스크: 본체 위치에 스텐실=1 기록 (색X)
        Pass
        {
            Name "Mask"
            Tags { "LightMode"="SRPDefaultUnlit" } // ★ URP가 실행
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
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float3 positionOS:POSITION; };
            struct V { float4 positionHCS:SV_POSITION; };
            V vert(A v){ V o; o.positionHCS = TransformObjectToHClip(v.positionOS); return o; }
            half4 frag(V i):SV_Target{ return 0; } // 안 그림
            ENDHLSL
        }

        // PASS 2) 아웃라인: 스텐실==1 위치에서만, Z가 본체보다 멀리 있을 때만 그림
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" } // ★ URP가 실행
            Cull Front
            ZWrite Off
            ZTest Greater      // ★ 본체보다 바깥쪽(더 먼)만
            ColorMask RGB
            Stencil
            {
                Ref 1
                Comp Equal      // ★ Mask 패스가 찍은 곳만 통과
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct A { float3 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionHCS:SV_POSITION; };

            V vert(A v)
            {
                V o;
                float3 posWS = TransformObjectToWorld(v.positionOS);
                float3 nWS  = normalize(TransformObjectToWorldNormal(v.normalOS));
                posWS += nWS * _OutlineWidth; // 외곽 확장
                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag(V i):SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
