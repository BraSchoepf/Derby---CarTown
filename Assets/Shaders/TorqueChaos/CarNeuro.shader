Shader "TorqueChaos/Neuro"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Background ("Background", Color) = (0,0,0,1)

        _Scale ("Scale", Range(0.01,10)) = 0.18
        _Speed ("Speed", Range(-5,5)) = 1.7
        _Phase ("Phase", Range(0,1)) = 1
        _Brightness ("Brightness", Range(0,5)) = 1

        [Toggle] _Pixelate ("Pixelate", Float) = 1

        _PixelSize ("Pixel Size", Range(1,128)) = 32

        _Metallic ("Metallic", Range(0,1)) = 0.7
        _Smoothness ("Smoothness", Range(0,1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "Neuro"

            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _Background;

                float _Scale;
                float _Speed;
                float _Phase;
                float _Brightness;

                float _Pixelate;
                float _PixelSize;

                float _Metallic;
                float _Smoothness;

            CBUFFER_END


            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );

                OUT.positionOS =
                    IN.positionOS.xyz;

                OUT.normalOS =
                    normalize(IN.normalOS);

                return OUT;
            }


            // ---------------------------------------------------------
            // ROTATION
            // ---------------------------------------------------------

            float2 Rotate(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                return mul(
                    float2x2(
                        c, s,
                        -s, c
                    ),
                    uv
                );
            }


            // ---------------------------------------------------------
            // NEURO SHAPE
            // ---------------------------------------------------------

            float NeuroShape(
                float2 uv,
                float t
            )
            {
                float2 sineAcc = float2(0,0);
                float2 result = float2(0,0);

                float scale = 8.0;

                [unroll]
                for(int j = 0; j < 15; j++)
                {
                    uv =
                        Rotate(
                            uv,
                            1.0
                        );

                    sineAcc =
                        Rotate(
                            sineAcc,
                            1.0
                        );

                    float2 layer =
                        uv * scale +
                        (float)j +
                        sineAcc -
                        t;

                    sineAcc +=
                        sin(layer);

                    result +=
                        (
                            0.5 +
                            0.5 *
                            cos(layer)
                        ) /
                        scale;

                    scale *= 1.2;
                }

                return result.x + result.y;
            }


            // ---------------------------------------------------------
            // NEURO PATTERN
            // ---------------------------------------------------------

            float3 NeuroPattern(
                float2 uv,
                float time
            )
            {
                uv -= 0.5;


                // -----------------------------------------------------
                // SCALE
                // -----------------------------------------------------

                float scale =
                    0.75 *
                    _Scale +
                    0.0001;

                uv *=
                    0.001 *
                    (
                        1.0 -
                        step(
                            1.0 - scale,
                            1.0
                        ) /
                        scale
                    );


                uv *= 100.0;


                // -----------------------------------------------------
                // TIME
                // -----------------------------------------------------

                float t =
                    time *
                    _Speed +
                    _Phase *
                    10.0;


                // -----------------------------------------------------
                // NEURO
                // -----------------------------------------------------

                float noise =
                    NeuroShape(
                        uv,
                        t
                    );


                noise =
                    _Brightness *
                    pow(
                        noise,
                        3.0
                    );


                noise +=
                    pow(
                        noise,
                        12.0
                    );


                noise =
                    max(
                        0.0,
                        noise -
                        0.5
                    );


                // -----------------------------------------------------
                // COLORS
                // -----------------------------------------------------

                float3 background =
                    _Background.rgb;

                float3 foreground =
                    _BaseColor.rgb;


                return lerp(
                    background,
                    foreground,
                    saturate(noise)
                );
            }


            // ---------------------------------------------------------
            // TRIPLANAR PROJECTION
            // ---------------------------------------------------------

            float3 ProjectX(
                float3 position,
                float time
            )
            {
                return NeuroPattern(
                    position.yz,
                    time
                );
            }


            float3 ProjectY(
                float3 position,
                float time
            )
            {
                return NeuroPattern(
                    position.xz,
                    time
                );
            }


            float3 ProjectZ(
                float3 position,
                float time
            )
            {
                return NeuroPattern(
                    position.xy,
                    time
                );
            }


            // ---------------------------------------------------------
            // FRAGMENT
            // ---------------------------------------------------------

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 position =
                    IN.positionOS;

                float3 normal =
                    normalize(
                        IN.normalOS
                    );


                // -----------------------------------------------------
                // TRIPLANAR BLEND
                // -----------------------------------------------------

                float3 blend =
                    abs(normal);

                blend =
                    pow(
                        blend,
                        6.0
                    );

                blend /=
                    max(
                        blend.x +
                        blend.y +
                        blend.z,
                        0.0001
                    );


                // -----------------------------------------------------
                // PROJECTIONS
                // -----------------------------------------------------

                float3 colorX =
                    ProjectX(
                        position,
                        _Time.y
                    );

                float3 colorY =
                    ProjectY(
                        position,
                        _Time.y + 11.0
                    );

                float3 colorZ =
                    ProjectZ(
                        position,
                        _Time.y + 23.0
                    );


                float3 finalColor =
                    colorX * blend.x +
                    colorY * blend.y +
                    colorZ * blend.z;


                // -----------------------------------------------------
                // PIXELATE
                // -----------------------------------------------------

                if(_Pixelate > 0.5)
                {
                    float3 pixelPos =
                        floor(
                            position *
                            _PixelSize
                        ) /
                        _PixelSize;

                    float3 pixelNormal =
                        normalize(
                            IN.normalOS
                        );

                    float3 pixelBlend =
                        pow(
                            abs(pixelNormal),
                            6.0
                        );

                    pixelBlend /=
                        max(
                            pixelBlend.x +
                            pixelBlend.y +
                            pixelBlend.z,
                            0.0001
                        );


                    float3 px =
                        ProjectX(
                            pixelPos,
                            _Time.y
                        );

                    float3 py =
                        ProjectY(
                            pixelPos,
                            _Time.y + 11.0
                        );

                    float3 pz =
                        ProjectZ(
                            pixelPos,
                            _Time.y + 23.0
                        );


                    finalColor =
                        px * pixelBlend.x +
                        py * pixelBlend.y +
                        pz * pixelBlend.z;
                }


                return half4(
                    saturate(finalColor),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}