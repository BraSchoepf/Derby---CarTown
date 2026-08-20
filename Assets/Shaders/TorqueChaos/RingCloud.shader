Shader "TorqueChaos/RingCloud"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Background ("Background", Color) = (0,0,0,1)

        _OuterColor ("Outer Color", Color) = (0.42,0.42,0.42,1)

        _Scale ("Scale", Range(0.05,5)) = 0.5
        _Cloudyness ("Cloudiness", Range(0.01,2)) = 0.3
        _Thickness ("Thickness", Range(0.05,2)) = 1
        _Speed ("Speed", Range(-5,5)) = 1

        _ProjectionScale ("Projection Scale", Range(0.05,5)) = 1

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
            Name "RingCloud"

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
                float4 _OuterColor;

                float _Scale;
                float _Cloudyness;
                float _Thickness;
                float _Speed;

                float _ProjectionScale;

                float _Metallic;
                float _Smoothness;

            CBUFFER_END


            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.positionOS = IN.positionOS.xyz;

                OUT.normalOS =
                    normalize(IN.normalOS);

                return OUT;
            }


            // ---------------------------------------------------------
            // RANDOM / NOISE
            // ---------------------------------------------------------

            float Random(float2 st)
            {
                return frac(
                    sin(
                        dot(
                            st,
                            float2(12.9898, 78.233)
                        )
                    ) * 43758.5453123
                );
            }


            float Noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);

                float a = Random(i);
                float b = Random(i + float2(1,0));
                float c = Random(i + float2(0,1));
                float d = Random(i + float2(1,1));

                float2 u =
                    f * f *
                    (3.0 - 2.0 * f);

                return lerp(
                    lerp(a,b,u.x),
                    lerp(c,d,u.x),
                    u.y
                );
            }


            float FBM(float2 n)
            {
                float total = 0.0;
                float amplitude = 0.4;

                [unroll]
                for(int i = 0; i < 8; i++)
                {
                    total += Noise(n) * amplitude;

                    n *= 2.0;

                    amplitude *= 0.6;
                }

                return total;
            }


            // ---------------------------------------------------------
            // RING
            // ---------------------------------------------------------

            float RingShape(
                float2 uv,
                float innerRadius,
                float outerRadius
            )
            {
                float distanceFromCenter =
                    length(uv);

                float lineWidth =
                    outerRadius - innerRadius;

                float ring =
                    smoothstep(
                        innerRadius,
                        innerRadius + 0.8 * lineWidth,
                        distanceFromCenter
                    );

                ring -=
                    smoothstep(
                        outerRadius,
                        outerRadius + 1.2 * lineWidth,
                        distanceFromCenter
                    );

                return saturate(ring);
            }


            // ---------------------------------------------------------
            // PATTERN
            // ---------------------------------------------------------

            float3 RingPattern(
                float2 uv,
                float time
            )
            {
                uv -= 0.5;

                uv *= _ProjectionScale;

                float safeScale =
                    max(_Scale, 0.001);

                uv *=
                    0.1 +
                    0.75 * safeScale;

                uv *= 3.0;


                float t =
                    time *
                    _Speed;


                float angle =
                    atan2(
                        uv.y,
                        uv.x
                    );


                float distanceFromCenter =
                    length(uv);


                float cloudScale =
                    max(
                        _Cloudyness,
                        0.01
                    );


                float2 polarUV =
                    float2(
                        angle,
                        0.1 * t
                        - 0.5 * distanceFromCenter
                        + 1.0 /
                        pow(
                            max(distanceFromCenter,0.001),
                            0.5
                        )
                    );


                polarUV *=
                    cloudScale;


                float noiseLeft =
                    FBM(
                        polarUV +
                        0.05 * t
                    );


                polarUV.x =
                    fmod(
                        polarUV.x,
                        cloudScale * 6.2831853
                    );


                float noiseRight =
                    FBM(
                        polarUV +
                        0.05 * t
                    );


                float noise =
                    lerp(
                        noiseRight,
                        noiseLeft,
                        smoothstep(
                            -0.2,
                            0.2,
                            uv.x
                        )
                    );


                // -------------------------------------------------
                // CENTER
                // -------------------------------------------------

                float centerShape =
                    1.0 -
                    pow(
                        smoothstep(
                            2.0,
                            0.0,
                            distanceFromCenter
                        ),
                        50.0
                    );


                // -------------------------------------------------
                // THICKNESS
                // -------------------------------------------------

                float thickness =
                    saturate(
                        _Thickness
                    );

                thickness =
                    thickness *
                    thickness;


                float radius =
                    0.4 -
                    0.25 *
                    thickness;


                float ring =
                    RingShape(
                        uv *
                        (
                            0.5 +
                            0.6 *
                            noise
                        ),
                        radius -
                        0.2 *
                        thickness,
                        radius +
                        0.5 *
                        thickness
                    );


                // -------------------------------------------------
                // INNER / OUTER
                // -------------------------------------------------

                float outer =
                    1.0 -
                    pow(
                        ring,
                        7.0
                    );

                outer *= ring;


                float inner =
                    ring -
                    outer;

                inner *= ring;


                // -------------------------------------------------
                // COLOR
                // -------------------------------------------------

                float3 background =
                    _Background.rgb;

                float3 outerColor =
                    _OuterColor.rgb;

                // _BaseColor controla el centro
                float3 innerColor =
                    _BaseColor.rgb;


                float3 color =
                    background *
                    (1.0 - ring);


                color +=
                    outerColor *
                    outer;


                color +=
                    innerColor *
                    inner;


                return saturate(color);
            }


            // ---------------------------------------------------------
            // TRIPLANAR
            // ---------------------------------------------------------

            float3 ProjectX(
                float3 position,
                float time
            )
            {
                return RingPattern(
                    position.yz * 0.5,
                    time
                );
            }


            float3 ProjectY(
                float3 position,
                float time
            )
            {
                return RingPattern(
                    position.xz * 0.5,
                    time
                );
            }


            float3 ProjectZ(
                float3 position,
                float time
            )
            {
                return RingPattern(
                    position.xy * 0.5,
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


                // TRIPLANAR BLEND
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


                // Tres proyecciones
                float3 colorX =
                    ProjectX(
                        position,
                        _Time.y
                    );


                float3 colorY =
                    ProjectY(
                        position,
                        _Time.y + 7.31
                    );


                float3 colorZ =
                    ProjectZ(
                        position,
                        _Time.y + 13.17
                    );


                // Mezcla
                float3 finalColor =
                    colorX * blend.x +
                    colorY * blend.y +
                    colorZ * blend.z;


                return half4(
                    saturate(finalColor),
                    1.0
                );
            }

            ENDHLSL
        }
    }
}