Shader "Vita/Debug/LightProbeOnly" {
    Properties{
        _MainTex("Diffuse Texture", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
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

                sampler2D _MainTex;
                sampler2D _BumpMap;
                float4 _MainTex_ST;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    half3 worldNormal : TEXCOORD1;
                };

                v2f vert(appdata v) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                    // Just pass world normal to fragment shader
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Normalize the interpolated normal (important!)
                    half3 worldNormal = normalize(i.worldNormal);

                    // Sample light probes per-pixel
                    half3 shLighting = ShadeSH9(half4(worldNormal, 1.0));

                    // Sample diffuse texture
                    half4 diffuse = tex2D(_MainTex, i.uv);

                    // Multiply by light probe lighting
                    half3 finalColor = diffuse.rgb * shLighting;

                    return half4(finalColor, 1.0);
                }
                ENDCG
            }
    }

        Fallback "Diffuse"
}