// 검 궤적 리본. 정점 색에 띠마다의 색과 흐려진 정도가 곱해져 들어오므로, 그 색을 그대로 배경에 더한다.
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb, 0);
            }
            ENDHLSL
        }
    }
}
