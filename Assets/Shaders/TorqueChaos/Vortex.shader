Shader "TorqueChaos/CarVortex"
{
    Properties
    {
        [Header(Vortex Colors)]
        _BaseColor ("Vortex Color", Color) = (0,0,0,1)
        _Background ("Background Color", Color) = (1,1,1,1)

        [Header(Vortex Shape)]
        _Scale ("Scale", Range(0.05,20)) = 0.5
        _Twist ("Twist", Range(0,5)) = 5
        _Segments ("Segments", Range(1,32)) = 20
        _Tightness ("Tightness", Range(0,1)) = 0.8
        _Softness ("Softness", Range(0,1)) = 0
        _Intensity ("Intensity", Range(0,2)) = 2

        [Header(Position)]
        _OffsetX ("Offset X", Range(-5,5)) = 0
        _OffsetZ ("Offset Z", Range(-5,5)) = -1.77

        [Header(Aspect)]
        _AspectX ("Aspect X", Range(0.1,5)) = 1
        _AspectY ("Aspect Y", Range(0.1,5)) = 1

        [Header(Animation)]
        _Speed ("Speed", Range(-10,10)) = 1
        _RotationSpeed ("Rotation Speed", Range(-10,10)) = 1

        [Header(Edge)]
        _Falloff ("Edge Falloff", Range(0.01,5)) = 5

        [Header(Metal)]
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
            Name "CarVortex"

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
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _Background;

                float _Scale;
                float _Twist;
                float _Segments;
                float _Tightness;
                float _Softness;
                float _Intensity;

                float _OffsetX;
                float _OffsetZ;

                float _AspectX;
                float _AspectY;

                float _Speed;
                float _RotationSpeed;

                float _Falloff;

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

                return OUT;
            }


            #define PI 3.14159265359
            #define TWO_PI 6.28318530718


            float Vortex(float2 p)
            {
                /*
                    CENTRO
                */

                p -= float2(0.5, 0.5);


                /*
                    ASPECT RATIO

                    Evita que el vortex se
                    convierta en una elipse.
                */

                p.x *= _AspectX;
                p.y *= _AspectY;


                /*
                    ESCALA
                */

                p *= _Scale;


                /*
                    DISTANCIA
                */

                float radius =
                    length(p);


                /*
                    ÁNGULO
                */

                float angle =
                    atan2(
                        p.y,
                        p.x
                    );


                /*
                    ANIMACIÓN
                */

                float time =
                    _Time.y *
                    _Speed;

                angle +=
                    time *
                    _RotationSpeed;


                /*
                    TWIST

                    Cuanto más lejos del centro,
                    más se modifica el ángulo.
                */

                angle +=
                    _Twist *
                    radius;


                /*
                    SEGMENTOS
                */

                float segmentedAngle =
                    angle *
                    _Segments;


                /*
                    FORMA RADIAL
                */

                float wave =
                    sin(
                        segmentedAngle
                    );


                /*
                    ESPIRAL

                    Combina radio + ángulo
                    para crear el efecto vortex.
                */

                float spiral =
                    sin(
                        segmentedAngle
                        +
                        radius *
                        5.0
                        -
                        time *
                        2.0
                    );


                /*
                    COMBINACIÓN
                */

                float pattern =
                    wave *
                    0.35
                    +
                    spiral *
                    0.65;


                /*
                    NORMALIZAR
                */

                pattern =
                    pattern *
                    0.5
                    +
                    0.5;


                /*
                    SUAVIZADO
                */

                float softness =
                    max(
                        _Softness,
                        0.001
                    );

                pattern =
                    smoothstep(
                        0.5 - softness,
                        0.5 + softness,
                        pattern
                    );


                /*
                    FALLOFF

                    Hace que desaparezca
                    progresivamente hacia afuera.
                */

                float edge =
                    1.0 -
                    smoothstep(
                        0.0,
                        max(_Falloff, 0.001),
                        radius
                    );


                /*
                    TIGHTNESS

                    Controla cuánto se concentra
                    el efecto alrededor del centro.
                */

                float tight =
                    pow(
                        saturate(edge),
                        lerp(
                            0.5,
                            5.0,
                            _Tightness
                        )
                    );


                /*
                    RESULTADO
                */

                float result =
                    pattern *
                    tight *
                    _Intensity;


                return saturate(result);
            }


            half4 Frag(Varyings IN) : SV_Target
            {
                /*
                    POSICIÓN DEL AUTO

                    XZ = plano horizontal.

                    Esto hace que el vortex quede
                    asociado al espacio del vehículo
                    y no a sus UV.
                */

                float2 pos =
                    IN.positionOS.xz;


                /*
                    CENTRO DEL VORTEX

                    Offset X y Z permiten moverlo
                    debajo del auto.
                */

                pos.x -= _OffsetX;
                pos.y -= _OffsetZ;


                /*
                    NORMALIZACIÓN SIMÉTRICA

                    Usamos la mayor dimensión para
                    evitar que el patrón se estire.
                */

                float largest =
                    max(
                        abs(pos.x),
                        abs(pos.y)
                    );

                largest =
                    max(
                        largest,
                        0.0001
                    );

                pos /=
                    largest;


                /*
                    Convertimos de -1..1
                    a 0..1
                */

                float2 uv =
                    pos *
                    0.5
                    +
                    0.5;


                /*
                    VORTEX
                */

                float mask =
                    Vortex(
                        uv
                    );


                /*
                    COLORES
                */

                float3 vortexColor =
                    _BaseColor.rgb;

                float3 backgroundColor =
                    _Background.rgb;


                /*
                    MEZCLA
                */

                float3 finalColor =
                    lerp(
                        backgroundColor,
                        vortexColor,
                        mask
                    );


                return half4(
                    finalColor,
                    1.0
                );
            }

            ENDHLSL
        }
    }
}