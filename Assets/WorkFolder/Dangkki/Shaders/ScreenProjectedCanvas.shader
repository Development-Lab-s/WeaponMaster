// 화면 공간 투영 셰이더.
// 캔버스를 그린 RenderTexture(_CanvasRT, 전역)를 "화면에서 보이는 위치 그대로" 표면에 얹는다.
// 모델 UV와 무관하게, 카메라 앞에 놓인 캔버스가 몸 표면에 그려진 것처럼 보인다.
Shader "Dangkki/ScreenProjectedCanvas"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _CanvasOpacity("Canvas Opacity", Range(0, 1)) = 1
        [Toggle] _CanvasUnlit("Canvas Ignores Lighting", Float) = 1
        [Toggle] _FlipY("Flip Canvas Y (if upside down)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // 전역 텍스처: ScreenProjectedCanvas 컴포넌트가 Shader.SetGlobalTexture로 넣어준다.
            TEXTURE2D(_CanvasRT);
            SAMPLER(sampler_CanvasRT);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _CanvasOpacity;
                half _CanvasUnlit;
                half _FlipY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 화면 픽셀 좌표 -> 0..1 화면 UV. 그 위치의 캔버스 픽셀을 그대로 가져온다.
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                if (_FlipY > 0.5h)
                    screenUV.y = 1.0 - screenUV.y;

                half4 canvas = SAMPLE_TEXTURE2D(_CanvasRT, sampler_CanvasRT, screenUV);
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * mainLight.shadowAttenuation * ndl + SampleSH(normalWS);

                half3 litBase = baseCol.rgb * lighting;
                half3 litCanvas = lerp(canvas.rgb * lighting, canvas.rgb, _CanvasUnlit);

                half alpha = saturate(canvas.a * _CanvasOpacity);
                half3 color = lerp(litBase, litCanvas, alpha);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
