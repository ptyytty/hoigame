Shader "Custom/Outline_Mobile_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,0.9,0.1,1)
        _OutlineWidth ("Outline Width (view-space)", Range(0.001,0.5)) = 0.06
    }

    SubShader
    {
        // [역할] URP 파이프라인/큐 지정 (모바일 빌드에서 안전하게 보이도록 Transparent+10)
        Tags {
            "RenderPipeline"="UniversalPipeline"       // ← 중요: 정확한 키!
            "Queue"="Transparent+10"
            "RenderType"="Transparent"
        }

        // [역할] 인버티드 헐 + Z-fight 방지
        Cull Front
        ZWrite Off
        ZTest LEqual
        Offset -1, -1

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "OUTLINE"

            // 🔸 내부선 제거 핵심: 원 오브젝트가 채운 Stencil==1 영역에서는 그리지 않음
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // [역할] 머티리얼 파라미터
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // [역할] 버텍스: 뷰공간 노멀 방향으로 살짝 확장 (거리 불변 느낌)
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // 오브젝트 → 월드
                float3 wPos = TransformObjectToWorld(v.vertex.xyz);
                float3 wNrm = TransformObjectToWorldNormal(v.normal);

                // 월드 → 뷰
                float3 viewPos = TransformWorldToView(wPos);
                float3 viewNrm = normalize(mul((float3x3)UNITY_MATRIX_V, wNrm));

                // 뷰공간에서 노멀 방향으로 확장
                viewPos += viewNrm * _OutlineWidth;

                // 뷰 → 클립
                o.pos = TransformWViewToHClip(viewPos);
                return o;
            }

            // [역할] 프래그먼트: 단색 아웃라인 출력
            half4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
