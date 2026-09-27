// 정면 투영 UV(uv2)로 캔버스 RenderTexture를 몸에 얹는 셰이더.
// PlanarCanvasUv 컴포넌트가 만든 uv2를 사용하므로 모델 원래 UV와 무관하게
// 정면에서 본 그림 그대로 몸에 붙고, 캐릭터가 돌아도 그림이 몸에 고정된다.
Shader "Dangkki/PlanarCanvasOnBody"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _CanvasTex("Canvas RenderTexture", 2D) = "black" {}
        _CanvasOpacity("Canvas Opacity", Range(0, 1)) = 1
        [Toggle] _CanvasUnlit("Canvas Ignores Lighting", Float) = 1
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
            TEXTURE2D(_CanvasTex);
            SAMPLER(sampler_CanvasTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _CanvasOpacity;
                half _CanvasUnlit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uvCanvas : TEXCOORD1; // PlanarCanvasUv 가 채운 uv2
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvCanvas : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
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
                output.uvCanvas = input.uvCanvas;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float2 cuv = input.uvCanvas;
                half4 canvas = SAMPLE_TEXTURE2D(_CanvasTex, sampler_CanvasTex, cuv);
                // 캔버스 영역(0..1) 밖은 아무것도 안 그린다.
                half inside = (cuv.x >= 0.0 && cuv.x <= 1.0 && cuv.y >= 0.0 && cuv.y <= 1.0) ? 1.0h : 0.0h;

                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * mainLight.shadowAttenuation * ndl + SampleSH(normalWS);

                half3 litBase = baseCol.rgb * lighting;
                half3 litCanvas = lerp(canvas.rgb * lighting, canvas.rgb, _CanvasUnlit);

                half alpha = saturate(canvas.a * _CanvasOpacity) * inside;
                return half4(lerp(litBase, litCanvas, alpha), 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
