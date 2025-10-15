Shader "Custom/OutlineMask_URP"
{
    SubShader
    {
        // URP + 불투명 큐에서 먼저 스텐실만 기록
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" "RenderType"="Opaque" }

        // [역할] 깊이는 정상 기록, 색은 기록 안 함
        ZWrite On
        ZTest LEqual
        ColorMask 0

        // [역할] 스텐실 ref=1 로 교체(=채움)
        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            Name "MASK"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex: POSITION; };
            struct v2f { float4 pos: SV_POSITION; };

            v2f vert(appdata v) { v2f o; o.pos = TransformObjectToHClip(v.vertex.xyz); return o; }
            half4 frag(v2f i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
