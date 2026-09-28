Shader "MapLighting/Soft Cloud Sky"
{
    Properties
    {
        _ZenithColor ("Upper Sky", Color) = (0.24, 0.55, 0.82, 1)
        _HorizonColor ("Horizon", Color) = (0.76, 0.86, 0.91, 1)
        _GroundColor ("Lower Hemisphere", Color) = (0.48, 0.47, 0.42, 1)
        _CloudColor ("Cloud Highlights", Color) = (1, 0.985, 0.95, 1)
        _CloudShade ("Cloud Underside", Color) = (0.72, 0.81, 0.88, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.84, 1)
        _Exposure ("Sky Exposure", Range(0, 3)) = 1.1
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.43
        _CloudScale ("Cloud Scale", Range(0.5, 8)) = 3.2
        _CloudSoftness ("Cloud Edge Softness", Range(0.01, 0.2)) = 0.065
        _CloudSpeed ("Cloud Drift", Range(0, 0.03)) = 0.003
        _SunSize ("Sun Angular Radius", Range(0.005, 0.1)) = 0.035
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _HorizonColor, _GroundColor;
                half4 _CloudColor, _CloudShade, _SunColor;
                float _Exposure, _CloudCoverage, _CloudScale;
                float _CloudSoftness, _CloudSpeed, _SunSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                            lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
            }

            // Broad silhouettes with small rounded lobes; no textures or scene geometry.
            float CloudNoise(float2 p)
            {
                float value = 0.57 * Noise(p);
                p = float2(p.x * 1.6 - p.y * 1.2, p.x * 1.2 + p.y * 1.6) + 7.3;
                value += 0.28 * Noise(p);
                p = float2(p.x * 1.6 - p.y * 1.2, p.x * 1.2 + p.y * 1.6) + 3.1;
                return value + 0.15 * Noise(p);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = normalize(input.direction);
                float elevation = saturate(direction.y);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(elevation, 0.55));
                sky = lerp(_GroundColor.rgb, sky, smoothstep(-0.16, 0.02, direction.y));

                float3 sunDirection = normalize(_MainLightPosition.xyz + float3(0, 0.00001, 0));
                float alignment = saturate(dot(direction, sunDirection));
                float sun = smoothstep(cos(_SunSize * 1.2), cos(_SunSize * 0.8), alignment);
                sky += _SunColor.rgb * (sun * 1.8 + pow(alignment, 64.0) * 0.12)
                     * smoothstep(-0.02, 0.04, direction.y);

                // Hemisphere projection avoids panorama seams and camera-position drift.
                float2 uv = direction.xz / (max(direction.y, 0.0) + 0.35) * _CloudScale;
                uv += float2(0.8, 0.35) * (_Time.y * _CloudSpeed) + float2(12.7, 4.2);
                float density = CloudNoise(uv);
                float threshold = lerp(0.82, 0.26, _CloudCoverage);
                float cloud = smoothstep(threshold - _CloudSoftness, threshold + _CloudSoftness, density);
                cloud *= smoothstep(0.025, 0.16, direction.y) * saturate(_CloudCoverage * 20.0);
                float fullness = smoothstep(threshold - 0.02, threshold + 0.18, density);
                half3 cloudColor = lerp(_CloudShade.rgb, _CloudColor.rgb, 0.42 + 0.58 * fullness);
                sky = lerp(sky, cloudColor, cloud);
                return half4(sky * _Exposure, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
