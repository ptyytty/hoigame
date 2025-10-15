Shader "Custom/Outline_Mobile_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,1,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.06
        _ZOffset      ("Depth Offset (units)", Float) = 1.0
        [Toggle(_OUTLINE_DEBUG_BYPASS)] _DebugBypass ("DEBUG: Bypass stencil & depth", Float) = 0 // ★추가
    }


    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" "RenderType"="Opaque" "IgnoreProjector"="True" }

        // --- PASS 0 : 원본 실루엣을 스텐실에 마킹(색은 안 그림) ---
        Pass
        {
            Name "Mask"
            Tags { "LightMode"="SRPDefaultUnlit" } // URP가 실행하도록

            Cull Back
            ZWrite On
            ZTest LEqual        // 기본 깊이 규칙
            ColorMask 0         // 화면 색은 변경 X

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace     // 원본이 그려진 픽셀을 스텐실=1로
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _OUTLINE_DEBUG_BYPASS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float3 positionOS:POSITION; };
            struct V { float4 positionHCS:SV_POSITION; };

            // [역할] 원본 그대로 위치로 투영(확장 없음)
            V vert(A v){
                V o; o.positionHCS = TransformObjectToHClip(v.positionOS); return o;
            }
            // [역할] 색은 출력하지 않음(스텐실만 설정)
            half4 frag(V i):SV_Target{ return 0; }
            ENDHLSL
        }

        // --- PASS 1 : 확장한(아웃라인) 메쉬를 "스텐실!=1" 영역에만 그림 ---
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite Off

            // ✅ 바꾼 부분: LEqual 유지 + 카메라 쪽으로 당김(음수 바이어스)
            ZTest Always
            Offset -4, -4     // ★ 깊이를 '조금 더 가깝게' 밀어넣어 LEqual 통과 안정화

            ColorMask RGB

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _ZOffset; // 사용 안 함(원하면 Offset 0, [_ZOffset]로 되돌려도 됨)
            CBUFFER_END

            struct A { float3 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionHCS:SV_POSITION; };

            V vert(A v)
            {
                V o;
                float3 posWS = TransformObjectToWorld(v.positionOS);
                float3 nWS   = normalize(TransformObjectToWorldNormal(v.normalOS));

                // [역할] 아웃라인 외곽 확장
                posWS += nWS * _OutlineWidth;

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
