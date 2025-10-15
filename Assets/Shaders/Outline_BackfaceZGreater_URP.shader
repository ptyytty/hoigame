Shader "Custom/Outline_BackfaceZGreater_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,1,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.08
    }

    SubShader
    {
        // 본체(Geometry) 그린 뒤에 렌더
        Tags { "Queue"="Geometry+1" "RenderType"="Opaque" "IgnoreProjector"="True" }

        // 인버티드 헐 1패스
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" } // ★ URP가 이 패스를 렌더하게 만듦

            Cull Back           // ★ 뒤(Backface)를 보이게
            ZWrite Off           // 덮어쓰기 방지
            ZTest Greater        // ★ 본체 깊이보다 "더 멀리" 있는 픽셀만 그림(겹침 방지)
            ColorMask RGB
            Blend Off

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

                // 노멀 방향으로 외곽 확장 (오브젝트 스페이스 기준)
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                posWS += normalize(nWS) * _OutlineWidth;

                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
