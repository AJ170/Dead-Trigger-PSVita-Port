Shader "Vita/Debug/LightProbeRaw" {
    Properties{
        _MainTex("Diffuse Texture", 2D) = "white" {}
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
                float4 _MainTex_ST;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    half3 worldNormal : TEXCOORD1;
                };

                v2f vert(appdata v) {
                    v2f o;

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                    // Just pass world normal
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Normalize the interpolated normal
                    half3 worldNormal = normalize(i.worldNormal);
                    half4 normalVec = half4(worldNormal, 1.0);

                    // Manual SH9 calculation per-pixel
                    half3 shColor;

                    // Linear terms
                    shColor.r = dot(unity_SHAr, normalVec);
                    shColor.g = dot(unity_SHAg, normalVec);
                    shColor.b = dot(unity_SHAb, normalVec);

                    // Quadratic terms
                    half4 normalSquared = normalVec.xyzz * normalVec.yzzx;
                    shColor.r += dot(unity_SHBr, normalSquared);
                    shColor.g += dot(unity_SHBg, normalSquared);
                    shColor.b += dot(unity_SHBb, normalSquared);

                    // Final quadratic term
                    half vC = worldNormal.x * worldNormal.x - worldNormal.y * worldNormal.y;
                    shColor += unity_SHC.xyz * vC;

                    // Sample texture
                    half4 diffuse = tex2D(_MainTex, i.uv);

                    // Combine
                    half3 finalColor = diffuse.rgb * shColor;

                    return half4(finalColor, 1.0);
                }
                ENDCG
            }
    }

        Fallback "Diffuse"
}