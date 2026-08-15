Shader "TorqueChaos/Starfall"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Background ("Background", Color) = (0.005,0.005,0.01,1)

        _Layers ("Star Layers", Range(1,20)) = 4
        _Scale ("Scale", Range(0.05,10)) = 0.53
        _Speed ("Speed", Range(0,5)) = 0.4

        _StarSize ("Star Size", Range(0.1,5)) = 1.2
        _Flare ("Star Flare", Range(0,5)) = 1.3
        _Brightness ("Brightness", Range(0,5)) = 1.2

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
            Name "Starfall"

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

                float _Layers;
                float _Scale;
                float _Speed;

                float _StarSize;
                float _Flare;
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


            float2x2 Rot(float a)
            {
                float s = sin(a);
                float c = cos(a);

                return float2x2(
                    c, -s,
                    s,  c
                );
            }


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


            float Star(
                float2 uv,
                float flare
            )
            {
                float d =
                    length(uv);

                d =
                    max(
                        d,
                        0.0001
                    );

                /*
                    Centro de la estrella
                */

                float m =
                    0.05 / d;


                /*
                    Rayos principales
                */

                float rays =
                    max(
                        0.0,
                        1.0 -
                        abs(
                            uv.x *
                            uv.y *
                            1000.0
                        )
                    );

                m +=
                    rays *
                    flare;


                /*
                    Rayos diagonales
                */

                uv =
                    mul(
                        Rot(
                            3.14159265 / 4.0
                        ),
                        uv
                    );

                rays =
                    max(
                        0.0,
                        1.0 -
                        abs(
                            uv.x *
                            uv.y *
                            1000.0
                        )
                    );

                m +=
                    rays *
                    0.3 *
                    flare;


                /*
                    Suavizado de la estrella
                */

                m *=
                    smoothstep(
                        1.0,
                        0.2,
                        d
                    );

                return m;
            }


            float3 StarLayer(
                float2 uv,
                float3 color,
                float timeOffset
            )
            {
                float3 col =
                    float3(0,0,0);

                float2 gv =
                    frac(uv) - 0.5;

                float2 id =
                    floor(uv);


                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offs =
                            float2(x,y);

                        float n =
                            Hash21(
                                id + offs
                            );

                        float size =
                            frac(
                                n *
                                1345.32
                            );

                        float flare =
                            smoothstep(
                                0.9,
                                1.0,
                                size
                            )
                            *
                            0.6 *
                            _Flare;


                        float star =
                            Star(
                                gv -
                                offs -
                                float2(
                                    n,
                                    frac(
                                        n * 34.0
                                    )
                                )
                                +
                                0.5,

                                flare
                            );


                        /*
                            Tamaño
                        */

                        star *=
                            _StarSize;


                        /*
                            Parpadeo individual
                        */

                        float twinkle =
                            sin(
                                _Time.y *
                                _Speed *
                                3.0 +
                                n *
                                6.323 +
                                timeOffset
                            )
                            *
                            0.5 +
                            1.0;

                        star *=
                            twinkle;


                        col +=
                            star *
                            size *
                            color;
                    }
                }

                return col;
            }


            float3 StarProjection(
                float2 uv,
                float3 color,
                float timeOffset
            )
            {
                /*
                    Centrar
                */

                uv -= 0.5;


                /*
                    Escala

                    Se mantiene independiente
                    del tamaño de las UV.
                */

                uv *=
                    (
                        3.0 /
                        max(
                            _Scale,
                            0.05
                        )
                    );


                /*
                    Rotación lenta
                */

                float t =
                    _Time.y *
                    _Speed *
                    0.05;

                uv =
                    mul(
                        Rot(t),
                        uv
                    );


                float3 result =
                    float3(0,0,0);


                int layerCount =
                    clamp(
                        (int)_Layers,
                        1,
                        20
                    );


                [loop]
                for (int i = 0; i < 20; i++)
                {
                    if (i >= layerCount)
                        break;


                    float fi =
                        (float)i;


                    /*
                        Profundidad
                    */

                    float depth =
                        frac(
                            fi /
                            max(
                                _Layers,
                                1.0
                            )
                            +
                            t
                        );


                    /*
                        Tamaño de la capa
                    */

                    float layerScale =
                        lerp(
                            20.0,
                            0.5,
                            depth
                        );


                    /*
                        Fade
                    */

                    float fade =
                        depth *
                        smoothstep(
                            1.0,
                            0.9,
                            depth
                        );


                    result +=
                        StarLayer(
                            uv *
                            layerScale +
                            fi *
                            455.2,

                            color,

                            fi *
                            10.0 +
                            timeOffset
                        )
                        *
                        fade;
                }


                return result;
            }


            half4 Frag(Varyings IN) : SV_Target
            {
                /*
                    ==================================================
                    TRIPLANAR OBJECT SPACE
                    ==================================================

                    Ya no usamos UV.

                    X projection:
                    usa YZ

                    Y projection:
                    usa XZ

                    Z projection:
                    usa XY
                */

                float3 position =
                    IN.positionOS;

                float3 normal =
                    normalize(
                        IN.normalOS
                    );


                /*
                    Peso de cada proyección.

                    Power 4 hace que cada cara use
                    principalmente su proyección
                    correspondiente.
                */

                float3 blend =
                    pow(
                        abs(normal),
                        4.0
                    );


                /*
                    Normalizamos los pesos
                */

                blend /=
                    max(
                        blend.x +
                        blend.y +
                        blend.z,

                        0.0001
                    );


                /*
                    Proyección X

                    Para caras orientadas hacia X:
                    usamos YZ.
                */

                float2 uvX =
                    position.yz;


                /*
                    Proyección Y

                    Para techo/capot:
                    usamos XZ.
                */

                float2 uvY =
                    position.xz;


                /*
                    Proyección Z

                    Para frente/lateral contrario:
                    usamos XY.
                */

                float2 uvZ =
                    position.xy;


                /*
                    Ajuste de coordenadas.

                    El patrón necesita coordenadas
                    consistentes alrededor del objeto.
                */

                uvX *= 0.5;
                uvY *= 0.5;
                uvZ *= 0.5;


                /*
                    STARFALL EN LAS TRES PROYECCIONES
                */

                float3 starsX =
                    StarProjection(
                        uvX,
                        _BaseColor.rgb,
                        0.0
                    );


                float3 starsY =
                    StarProjection(
                        uvY,
                        _BaseColor.rgb,
                        100.0
                    );


                float3 starsZ =
                    StarProjection(
                        uvZ,
                        _BaseColor.rgb,
                        200.0
                    );


                /*
                    Mezcla Triplanar
                */

                float3 stars =
                    starsX * blend.x +
                    starsY * blend.y +
                    starsZ * blend.z;


                /*
                    Brillo
                */

                stars *=
                    _Brightness;


                /*
                    BACKGROUND
                */

                float3 finalColor =
                    _Background.rgb +
                    stars;


                return half4(
                    saturate(finalColor),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}