// 몸 그리기용 셰이더. 베이스맵을 UV0이 아니라 UV2(라이트맵 UV, TEXCOORD1)로 읽는다.
// 라이트맵 UV는 겹치는 곳이 없어서 몸 어디에 그려도 다른 곳에 같이 찍히지 않는다.
// (FBX 임포트 설정 Generate Lightmap UVs 필요)
Shader "Dangkki/BodyPaintLit"
{
    Properties
    {
        _BaseMap("Base Map (UV2)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        // "RenderPipeline" 태그는 일부러 뺐다. 유니티 시작 직후(URP 로드 전)에 임포트되면
        // 이 태그 때문에 서브셰이더가 통째로 제거되고, 그 결과가 캐시에 남아 셰이더가 안 먹는다.
        Tags
        {
            "RenderType" = "Opaque"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uvPaint : TEXCOORD1; // 라이트맵 UV
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
                output.uv = TRANSFORM_TEX(input.uvPaint, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * mainLight.shadowAttenuation * ndl + SampleSH(normalWS);

                return half4(albedo.rgb * lighting, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    // FallBack 없음: Lit으로 대체되면 원래 UV0으로 그려져서 그림이 엉뚱하게 번진다. 실패하면 차라리 분홍색으로 보이게.
}
