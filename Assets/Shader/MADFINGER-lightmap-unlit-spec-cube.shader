Shader "MADFINGER/Environment/Cubemap specular + Lightmap" {
    Properties{
        _MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
        _SpecCubeTex("SpecCube", CUBE) = "black" {}
        _SpecularStrength("Specular Strength", Range(0, 2)) = 1.0
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _ScrollingSpeed("Scrolling speed", Vector) = (0,0,0,0)
    }
        SubShader{
            LOD 100
            Tags { "LIGHTMODE" = "ForwardBase" "RenderType" = "Opaque" }
            Pass {
                Tags { "LIGHTMODE" = "ForwardBase" "RenderType" = "Opaque" }
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                float4 _ScrollingSpeed;
                float4 _MainTex_ST;
                sampler2D _MainTex;
                samplerCUBE _SpecCubeTex;
                half _SpecularStrength;
                half _Roughness;

                struct appdata_t {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    float4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    float4 color : COLOR;
                    half3 worldNormal : TEXCOORD2;
                    half3 worldViewDir : TEXCOORD3;
                };

                v2f vert(appdata_t v) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    o.pos = UnityObjectToClipPos(v.vertex);

                    // Scrolling UVs for main texture
                    o.uv = (v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
                    o.uv += frac(_ScrollingSpeed.xy * _Time.y);

                    // Lightmap UVs
                    o.uv1 = (v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw;

                    // Vertex color
                    o.color = v.color;

                    // World space normal for reflection
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);

                    // World space view direction for reflection
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Sample base texture
                    half4 baseColor = tex2D(_MainTex, i.uv);

                    // Decode lightmap
                    half3 lightmap = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1));

                    // Apply lightmap to base color
                    half3 color = baseColor.rgb * (1.0 * lightmap);

                    // Apply vertex color modulation
                    color *= i.color.rgb;

                    // === CUBEMAP REFLECTION ===

                    // Normalize interpolated values
                    half3 worldNormal = normalize(i.worldNormal);
                    half3 worldViewDir = normalize(i.worldViewDir);

                    // Calculate reflection vector
                    half3 reflectionVector = reflect(-worldViewDir, worldNormal);

                    // Sample cubemap with roughness LOD
                    half roughnessLOD = _Roughness * 7.0;
                    half4 cubeColor = texCUBElod(_SpecCubeTex, half4(reflectionVector, roughnessLOD));

                    // Use alpha channel (gloss) as reflection strength
                    // Combine with _SpecularStrength property
                    half reflectionAmount = baseColor.a * _SpecularStrength;

                    // Fresnel effect (stronger reflections at glancing angles)
                    half NdotV = saturate(dot(worldNormal, worldViewDir));
                    half fresnel = pow(1.0 - NdotV, 2.0);
                    reflectionAmount *= fresnel;

                    // Blend reflection with base color
                    color = lerp(color, color + cubeColor.rgb, reflectionAmount);

                    return half4(color, baseColor.a);
                }
                ENDCG
            }
        }
}