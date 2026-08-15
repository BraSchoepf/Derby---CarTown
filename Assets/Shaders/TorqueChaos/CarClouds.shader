Shader "TorqueChaos/Clouds"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5,0.5,0.5,1)
        _Background ("Background", Color) = (0.02,0.02,0.025,1)

        _Speed ("Speed", Range(0,5)) = 0.2
        _Scale ("Scale", Range(0.05,10)) = 5

        _Light ("Cloud Light", Range(0,2)) = 2
        _Shadow ("Cloud Shadow", Range(0,2)) = 0.42
        _Tint ("Cloud Tint", Range(0,2)) = 0.93

        _Coverage ("Coverage", Range(0,2)) = 0
        _Alpha ("Alpha", Range(0,2)) = 2

        _CloudBrightness ("Cloud Brightness", Range(0,3)) = 2.24

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
            Name "Clouds"

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

                float _Speed;
                float _Scale;

                float _Light;
                float _Shadow;
                float _Tint;

                float _Coverage;
                float _Alpha;

                float _CloudBrightness;

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
            // MATRIX
            // =====================================================

            static const float2x2 NoiseMatrix =
                float2x2(
                    1.6,
                    1.2,
                   -1.2,
                    1.6
                );


            // =====================================================
            // HASH
            // =====================================================

            float2 Hash(float2 p)
            {
                p =
                    float2(
                        dot(
                            p,
                            float2(
                                127.1,
                                311.7
                            )
                        ),

                        dot(
                            p,
                            float2(
                                269.5,
                                183.3
                            )
                        )
                    );

                return
                    -1.0 +
                    2.0 *
                    frac(
                        sin(p) *
                        43758.5453123
                    );
            }


            // =====================================================
            // SIMPLEX NOISE
            // =====================================================

            float Noise(float2 p)
            {
                const float K1 =
                    0.366025404;

                const float K2 =
                    0.211324865;


                float2 i =
                    floor(
                        p +
                        (p.x + p.y) *
                        K1
                    );


                float2 a =
                    p -
                    i +
                    (i.x + i.y) *
                    K2;


                float2 o =
                    (
                        a.x >
                        a.y
                    )
                    ?
                    float2(1.0,0.0)
                    :
                    float2(0.0,1.0);


                float2 b =
                    a -
                    o +
                    K2;


                float2 c =
                    a -
                    1.0 +
                    2.0 *
                    K2;


                float3 h =
                    max(
                        0.5 -
                        float3(
                            dot(a,a),
                            dot(b,b),
                            dot(c,c)
                        ),

                        0.0
                    );


                float3 n =
                    h *
                    h *
                    h *
                    h *
                    float3(
                        dot(
                            a,
                            Hash(i)
                        ),

                        dot(
                            b,
                            Hash(i + o)
                        ),

                        dot(
                            c,
                            Hash(i + 1.0)
                        )
                    );


                return
                    dot(
                        n,
                        float3(
                            70.0,
                            70.0,
                            70.0
                        )
                    );
            }


            // =====================================================
            // FBM
            // =====================================================

            float FBM(float2 n)
            {
                float total = 0.0;

                float amplitude =
                    0.1;


                [unroll]
                for (int i = 0; i < 7; i++)
                {
                    total +=
                        Noise(n) *
                        amplitude;

                    n =
                        mul(
                            NoiseMatrix,
                            n
                        );

                    amplitude *=
                        0.4;
                }


                return total;
            }


            // =====================================================
            // CLOUD GENERATOR
            // =====================================================

            float3 CloudPattern(
                float2 uv,
                float time
            )
            {
                /*
                    Escala
                */

                float scale =
                    max(
                        _Scale,
                        0.001
                    );


                uv *=
                    scale *
                    0.5;


                /*
                    Movimiento
                */

                float q =
                    FBM(uv);


                /*
                    RIDGED NOISE
                */

                float r =
                    0.0;

                float2 ridgedUV =
                    uv;

                ridgedUV -=
                    q -
                    time;


                float weight =
                    0.8;


                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    r +=
                        abs(
                            weight *
                            Noise(
                                ridgedUV
                            )
                        );

                    ridgedUV =
                        mul(
                            NoiseMatrix,
                            ridgedUV
                        )
                        +
                        time;

                    weight *=
                        0.7;
                }


                /*
                    NORMAL CLOUD NOISE
                */

                float f =
                    0.0;

                float2 cloudUV =
                    uv;

                cloudUV -=
                    q -
                    time;

                weight =
                    0.7;


                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    f +=
                        weight *
                        Noise(
                            cloudUV
                        );

                    cloudUV =
                        mul(
                            NoiseMatrix,
                            cloudUV
                        )
                        +
                        time;

                    weight *=
                        0.6;
                }


                f *=
                    r +
                    f;


                /*
                    COLOR NOISE
                */

                float c =
                    0.0;

                float2 colorUV =
                    uv *
                    2.0;

                colorUV -=
                    q -
                    time *
                    2.0;

                weight =
                    0.4;


                [unroll]
                for (int i = 0; i < 7; i++)
                {
                    c +=
                        weight *
                        Noise(
                            colorUV
                        );

                    colorUV =
                        mul(
                            NoiseMatrix,
                            colorUV
                        )
                        +
                        time *
                        2.0;

                    weight *=
                        0.6;
                }


                /*
                    RIDGED COLOR
                */

                float c1 =
                    0.0;

                float2 ridgeColorUV =
                    uv *
                    3.0;

                ridgeColorUV -=
                    q -
                    time *
                    3.0;

                weight =
                    0.4;


                [unroll]
                for (int i = 0; i < 7; i++)
                {
                    c1 +=
                        abs(
                            weight *
                            Noise(
                                ridgeColorUV
                            )
                        );

                    ridgeColorUV =
                        mul(
                            NoiseMatrix,
                            ridgeColorUV
                        )
                        +
                        time *
                        3.0;

                    weight *=
                        0.6;
                }


                c +=
                    c1;


                /*
                    CLOUD COLOR

                    Sempre branco.
                */

                float3 cloudColor =
                    float3(
                        1.0,
                        1.0,
                        1.0
                    );


                cloudColor *=
                    saturate(
                        (
                            1.0 -
                            _Shadow
                        )
                        +
                        _Light *
                        c
                    );


                cloudColor *=
                    _CloudBrightness;


                /*
                    Cobertura
                */

                float cloudMask =
                    _Coverage +
                    20.0 *
                    _Alpha *
                    f *
                    r;


                cloudMask =
                    saturate(
                        cloudMask +
                        c
                    );


                /*
                    CÉU

                    _BaseColor controla
                    a cor principal.
                */

                float3 skyColor =
                    lerp(
                        _Background.rgb,
                        _BaseColor.rgb,
                        0.5
                    );


                /*
                    Mistura final
                */

                float3 result =
                    lerp(
                        skyColor,
                        saturate(
                            _Tint *
                            skyColor +
                            cloudColor
                        ),
                        cloudMask
                    );


                return result;
            }


            // =====================================================
            // FRAGMENT
            // =====================================================

            half4 Frag(Varyings IN) : SV_Target
            {
                /*
                    =================================================
                    TRIPLANAR OBJECT SPACE
                    =================================================

                    X → YZ
                    Y → XZ
                    Z → XY
                */

                float3 position =
                    IN.positionOS;

                float3 normal =
                    normalize(
                        IN.normalOS
                    );


                /*
                    Peso de cada proyección.

                    4 = bastante definido.
                */

                float3 blend =
                    pow(
                        abs(normal),
                        4.0
                    );


                blend /=
                    max(
                        blend.x +
                        blend.y +
                        blend.z,

                        0.0001
                    );


                /*
                    PROJEÇÃO X
                    Laterais
                */

                float2 uvX =
                    position.yz;


                /*
                    PROJEÇÃO Y
                    Techo / capó
                */

                float2 uvY =
                    position.xz;


                /*
                    PROJEÇÃO Z
                    Frente / trasera
                */

                float2 uvZ =
                    position.xy;


                /*
                    Mesma escala para
                    todas as direcciones.
                */

                uvX *= 0.5;
                uvY *= 0.5;
                uvZ *= 0.5;


                /*
                    Tiempo ligeramente diferente
                    por proyección para evitar que
                    las costuras sean demasiado obvias.
                */

                float time =
                    _Time.y *
                    _Speed *
                    0.1;


                float3 cloudX =
                    CloudPattern(
                        uvX,
                        time
                    );


                float3 cloudY =
                    CloudPattern(
                        uvY,
                        time
                    );


                float3 cloudZ =
                    CloudPattern(
                        uvZ,
                        time
                    );


                /*
                    TRIPLANAR BLEND
                */

                float3 finalColor =
                    cloudX * blend.x +
                    cloudY * blend.y +
                    cloudZ * blend.z;


                return half4(
                    saturate(finalColor),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}