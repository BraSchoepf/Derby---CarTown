Shader "TorqueChaos/Pixel"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Background ("Background", Color) = (0.03,0.03,0.03,1)

        _Blur ("Blur", Range(0.01,2)) = 0.15    
        _Animate ("Animate", Float) = 1
        _AnimateSpeed ("Animation Speed", Range(0,5)) = 2
        _Frequency ("Frequency", Range(0.05,5)) = 5

        _Scale ("Scale", Range(0.05,10)) = 0.3
        _Distortion ("Distortion", Range(0,5)) = 1.5
        _Brightness ("Brightness", Range(0,3)) = 2.5

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
            Name "Pixel"

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

                float _Blur;
                float _Animate;
                float _AnimateSpeed;
                float _Frequency;

                float _Scale;
                float _Distortion;
                float _Brightness;

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


            // =====================================================
            // ROTACIÓN
            // =====================================================

            float2x2 Rotate2D(float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                return float2x2(
                    c, -s,
                    s,  c
                );
            }


            // =====================================================
            // HASH
            // =====================================================

            float Hash21(float2 p)
            {
                p =
                    frac(
                        p *
                        float2(
                            123.34,
                            456.21
                        )
                    );

                p +=
                    dot(
                        p,
                        p + 45.32
                    );

                return frac(
                    p.x * p.y
                );
            }


            // =====================================================
            // NOISE
            // =====================================================

            float Noise(float2 p)
            {
                float2 i =
                    floor(p);

                float2 f =
                    frac(p);

                float2 u =
                    f * f *
                    (3.0 - 2.0 * f);

                float a =
                    Hash21(
                        i +
                        float2(0,0)
                    );

                float b =
                    Hash21(
                        i +
                        float2(1,0)
                    );

                float c =
                    Hash21(
                        i +
                        float2(0,1)
                    );

                float d =
                    Hash21(
                        i +
                        float2(1,1)
                    );

                float x1 =
                    lerp(
                        a,
                        b,
                        u.x
                    );

                float x2 =
                    lerp(
                        c,
                        d,
                        u.x
                    );

                return lerp(
                    x1,
                    x2,
                    u.y
                );
            }


            // =====================================================
            // PIXEL PATTERN
            // =====================================================

            float3 PixelPattern(
                float2 uv,
                float time
            )
            {
                float scale =
                    max(
                        _Scale,
                        0.01
                    );

                uv *=
                    scale;


                /*
                    Animación
                */

                float speed =
                    time *
                    _AnimateSpeed *
                    _Animate;


                /*
                    Rotación
                */

                float n =
                    Noise(
                        float2(
                            speed * 0.1,
                            uv.x * uv.y
                        )
                    );

                float angle =
                    (n - 0.5) *
                    6.2831853;

                uv =
                    mul(
                        Rotate2D(angle),
                        uv
                    );


                /*
                    Distorsión
                */

                float frequency =
                    20.0 *
                    _Frequency;

                float amplitude =
                    max(
                        0.1,
                        30.0 *
                        (
                            10.0 *
                            (
                                0.01 +
                                _Blur
                            )
                        )
                    );


                uv.x +=
                    sin(
                        uv.y *
                        frequency +
                        speed
                    )
                    /
                    amplitude *
                    _Distortion;


                uv.y +=
                    sin(
                        uv.x *
                        frequency *
                        1.5 +
                        speed
                    )
                    /
                    (
                        amplitude *
                        0.5
                    )
                    *
                    _Distortion;


                /*
                    Crear patrón
                */

                float pattern =
                    Noise(
                        uv *
                        4.0
                    );


                /*
                    Contraste
                */

                pattern =
                    smoothstep(
                        0.25,
                        0.75,
                        pattern
                    );


                /*
                    BaseColor
                    = color del patrón

                    Background
                    = fondo
                */

                float3 result =
                    lerp(
                        _Background.rgb,
                        _BaseColor.rgb,
                        pattern
                    );


                return
                    result *
                    _Brightness;
            }


            // =====================================================
            // FRAGMENT
            // =====================================================

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 position =
                    IN.positionOS;

                float3 normal =
                    normalize(
                        IN.normalOS
                    );


                /*
                    =================================================
                    TRIPLANAR
                    =================================================

                    X = laterales
                    Y = techo / capó
                    Z = frente / atrás
                */

                float3 blend =
                    abs(normal);


                blend =
                    blend /
                    max(
                        blend.x +
                        blend.y +
                        blend.z,
                        0.0001
                    );


                /*
                    PROYECCIONES
                */

                float2 uvX =
                    position.yz;

                float2 uvY =
                    position.xz;

                float2 uvZ =
                    position.xy;


                /*
                    Escala base.
                */

                uvX *= 0.5;
                uvY *= 0.5;
                uvZ *= 0.5;


                /*
                    Tiempo
                */

                float time =
                    _Time.y;


                /*
                    Tres patrones
                */

                float3 pixelX =
                    PixelPattern(
                        uvX,
                        time
                    );

                float3 pixelY =
                    PixelPattern(
                        uvY,
                        time
                    );

                float3 pixelZ =
                    PixelPattern(
                        uvZ,
                        time
                    );


                /*
                    Mezcla Triplanar
                */

                float3 finalColor =
                    pixelX * blend.x +
                    pixelY * blend.y +
                    pixelZ * blend.z;


                return half4(
                    saturate(finalColor),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}