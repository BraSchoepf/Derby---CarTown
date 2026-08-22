Shader "TorqueChaos/Halftone"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Background ("Background", Color) = (0.05,0.05,0.05,1)

        _Scale ("Dot Scale", Range(0.1,20)) = 20
        _DotSize ("Dot Size", Range(0.02,0.5)) = 0.15

        _Blur ("Dot Softness", Range(0.01,1)) = 0.25

        _Animate ("Animate", Float) = 10
        _AnimateSpeed ("Animation Speed", Range(0,5)) = 5

        _Frequency ("Frequency", Range(0.01,5)) = 0.37
        _Distortion ("Distortion", Range(0,1)) = 0.0

        _TriplanarSharpness ("Triplanar Sharpness", Range(1,16)) = 7.5

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
            Name "Halftone"

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
                float _DotSize;
                float _Blur;

                float _Animate;
                float _AnimateSpeed;

                float _Frequency;
                float _Distortion;

                float _TriplanarSharpness;

                float _Metallic;
                float _Smoothness;

            CBUFFER_END


            // =====================================================
            // VERTEX
            // =====================================================

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

                return
                    frac(
                        p.x *
                        p.y
                    );
            }


            // =====================================================
            // CIRCULAR HALFTONE
            // =====================================================

            float HalftoneDot(
                float2 uv,
                float time
            )
            {
                /*
                    Escala del patrón
                */

                uv *=
                    max(
                        _Scale,
                        0.01
                    );


                /*
                    Animación muy suave
                */

                float animation =
                    time *
                    _AnimateSpeed *
                    _Animate;


                /*
                    Movimiento opcional.
                    No altera la proporción.
                */

                uv +=
                    float2(
                        animation * 0.05,
                        animation * 0.02
                    );


                /*
                    Grid de puntos
                */

                float2 grid =
                    floor(uv);

                float2 cell =
                    frac(uv) -
                    0.5;


                /*
                    Random por celda
                */

                float random =
                    Hash21(grid);


                /*
                    Tamaño del punto
                */

                float dotSize =
                    _DotSize *
                    lerp(
                        0.75,
                        1.15,
                        random
                    );


                /*
                    Distancia circular.

                    IMPORTANTE:
                    usamos length() directamente.
                    Esto mantiene el punto circular.
                */

                float distanceToCenter =
                    length(cell);


                /*
                    Suavizado del borde
                */

                float softness =
                    max(
                        _Blur * 0.05,
                        0.002
                    );


                float dot =
                    1.0 -
                    smoothstep(
                        dotSize,
                        dotSize + softness,
                        distanceToCenter
                    );


                return saturate(dot);
            }


            // =====================================================
            // TRIPLANAR
            // =====================================================

            float3 TriplanarHalftone(
                float3 position,
                float3 normal,
                float time
            )
            {
                /*
                    Pesos según la normal.

                    Cada superficie utiliza principalmente
                    su proyección correspondiente.
                */

                float3 blend =
                    pow(
                        abs(normal),
                        _TriplanarSharpness
                    );


                blend /=
                    max(
                        blend.x +
                        blend.y +
                        blend.z,
                        0.0001
                    );


                /*
                    X

                    Proyección sobre YZ.
                    Principalmente para los laterales.
                */

                float2 uvX =
                    float2(
                        position.z,
                        position.y
                    );


                /*
                    Y

                    Proyección sobre XZ.
                    Techo, capó y superficies superiores.
                */

                float2 uvY =
                    float2(
                        position.x,
                        position.z
                    );


                /*
                    Z

                    Proyección sobre XY.
                    Frente y parte trasera.
                */

                float2 uvZ =
                    float2(
                        position.x,
                        position.y
                    );


                /*
                    IMPORTANTE:

                    Cada plano recibe la misma escala.

                    No usamos las UV del modelo.
                    Por eso una puerta no debería
                    estirar los puntos.
                */

                float dotsX =
                    HalftoneDot(
                        uvX,
                        time
                    );

                float dotsY =
                    HalftoneDot(
                        uvY,
                        time
                    );

                float dotsZ =
                    HalftoneDot(
                        uvZ,
                        time
                    );


                /*
                    Mezcla triplanar
                */

                float dots =
                    dotsX * blend.x +
                    dotsY * blend.y +
                    dotsZ * blend.z;


                return dots.xxx;
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
                    Generamos los puntos
                */

                float dots =
                    TriplanarHalftone(
                        position,
                        normal,
                        _Time.y
                    ).r;


                /*
                    Color del auto
                    =
                    puntos

                    Background
                    =
                    espacios entre puntos
                */

                float3 finalColor =
                    lerp(
                        _Background.rgb,
                        _BaseColor.rgb,
                        dots
                    );


                return half4(
                    saturate(finalColor),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}