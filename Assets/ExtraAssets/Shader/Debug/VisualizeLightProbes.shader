Shader "Vita/Debug/VisualizeLightProbes" {
    Properties{
        _Scale("Visualization Scale", Float) = 1.0
    }

        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "UnityCG.cginc"

                float _Scale;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    half3 worldNormal : TEXCOORD0;
                };

                v2f vert(appdata v) {
                    v2f o;

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    half3 worldNormal = normalize(i.worldNormal);

                    // Sample light probes
                    half3 shLighting = ShadeSH9(half4(worldNormal, 1.0));

                    // Visualize as color
                    // If it's (0.2, 0.2, 0.2) = dark gray, probes have no data
                    // If it's (0.5+, 0.5+, 0.5+) = bright, probes are working
                    half3 visualized = shLighting * _Scale;

                    return half4(visualized, 1.0);
                }
                ENDCG
            }
    }

        Fallback "Diffuse"
}