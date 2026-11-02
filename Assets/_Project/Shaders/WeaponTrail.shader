// 검 궤적 리본. 정점 색에 띠마다의 색과 흐려진 정도가 곱해져 들어오므로, 그 색을 배경에 더한다.
// 폭 위치(UV y, 손잡이 0 ~ 칼끝 1)를 감쇠 지수(UV z)만큼 거듭제곱해 곱하므로, 밝기가 칼끝에 모이고 손잡이 쪽으로 연해진다.
// 정점 두 개 사이의 선형 보간으로는 곡선 감쇠를 낼 수 없어 픽셀 단계에서 계산한다.
// 칼을 어느 쪽에서 봐도 보이도록 앞뒷면을 모두 그리고, 깊이를 쓰지 않아 띠끼리 겹쳐도 서로 가리지 않는다.
// 그림자와 깊이·노멀 패스를 두지 않아, 띠가 그림자를 만들거나 SSAO가 띠를 표면으로 읽지 않는다.
Shader "GodOfWar/WeaponTrail"
{
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
            Name "WeaponTrail"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // pow(0, 0)은 정의되지 않으므로 폭 위치를 0보다 조금 크게 잡는다.
                half width = pow(max(input.uv.y, 1e-4), input.uv.z);
                return half4(input.color.rgb * width, 0);
            }
            ENDHLSL
        }
    }
}
