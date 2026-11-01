// 피격 반짝임 오버레이. 원래 머티리얼로 그린 표면 위에 같은 메쉬를 한 번 더 그려 색을 더한다.
// 원래 표면의 색과 셰이더에 관계없이 보이도록 더하기로 섞고, 원래 표면이 쓴 깊이를 통과만 한다.
// 그림자와 깊이·노멀 패스를 두지 않아, 그림자가 겹치거나 SSAO가 오버레이를 표면으로 읽지 않는다.
Shader "GodOfWar/HitFlashOverlay"
{
    Properties
    {
        [HDR] _FlashColor ("Flash Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HitFlash"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher가 머티리얼마다 값만 바꿔 같은 배치로 그리도록 UnityPerMaterial에 둔다.
            CBUFFER_START(UnityPerMaterial)
                half4 _FlashColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(_FlashColor.rgb, 0);
            }
            ENDHLSL
        }
    }
}
