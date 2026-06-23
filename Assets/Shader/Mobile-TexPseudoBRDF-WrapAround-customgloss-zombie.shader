Shader "Vita/Character/BRDFLit Backlight - Zombie Shader" {
    Properties{
        _MainTex("Base (RGB) Gloss (A)", 2D) = "grey" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BRDFTex("BRDF Lookup (NdotL, NdotH)", 2D) = "white" {}

        _LightProbesLightingAmount("Light Probes Lighting Amount", Range(0, 1)) = 0.9
        _SpecularStrength("Specular Strength Weights", Vector) = (0, 0, 0, 1)
    }

        SubShader{
            Tags { "RenderType" = "Opaque" "LIGHTMODE" = "ForwardBase" }
            LOD 400

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #pragma multi_compile_fwdbase
                #pragma multi_compile_fog

                #define UNITY_SKINNED_MESH

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _BumpMap;
                sampler2D _BRDFTex;

                float4 _MainTex_ST;
                float4 _SpecularStrength;
                float _LightProbesLightingAmount;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 uv : TEXCOORD0;
                    float4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    half3 lightDir : TEXCOORD1;      // Direction to main light (backlight)
                    half3 halfVector : TEXCOORD2;    // Half vector for specular
                    half4 lighting : TEXCOORD3;      // SH9 lighting + bias
                    UNITY_FOG_COORDS(4)
                };

                // Calculate Spherical Harmonics lighting
                float3 SampleSH9(float3 normal) {
                    float4 normalVec = float4(normal, 1.0);

                    // Linear terms
                    float3 x1;
                    x1.x = dot(unity_SHAr, normalVec);
                    x1.y = dot(unity_SHAg, normalVec);
                    x1.z = dot(unity_SHAb, normalVec);

                    // Quadratic terms
                    float4 normalSquared = normalVec.xyzz * normalVec.yzzx;
                    float3 x2;
                    x2.x = dot(unity_SHBr, normalSquared);
                    x2.y = dot(unity_SHBg, normalSquared);
                    x2.z = dot(unity_SHBb, normalSquared);

                    // Final quadratic term
                    float vC = normal.x * normal.x - normal.y * normal.y;
                    float3 x3 = unity_SHC.xyz * vC;

                    return x1 + x2 + x3;
                }

                v2f vert(appdata v) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    // Transform to clip space
                    o.pos = UnityObjectToClipPos(v.vertex);

                    // UVs
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                    // Transform normal to world space
                    float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                    // Get light probe lighting
                    float3 shLighting = SampleSH9(worldNormal);

                    // Apply bias: ensures minimum lighting based on _LightProbesLightingAmount
                    // High value (0.9) = mostly probe lighting with 10% ambient fill
                    // Low value (0.2) = mostly ambient fill with probe lighting on top
                    float lightingBias = 1.0 - _LightProbesLightingAmount;
                    shLighting = clamp(shLighting + lightingBias, 0.0, 1.0);

                    o.lighting = float4(shLighting, 1.0);

                    // Calculate direction to light (for backlight/rim lighting)
                    // This uses the dominant direction from the SH coefficients
                    float3 dominantLight = normalize(
                        (unity_SHAr.xyz * 0.3) +
                        (unity_SHAg.xyz * 0.59) +
                        (unity_SHAb.xyz * 0.11)
                    );

                    // Transform dominant light direction to tangent space
                    float3x3 tangentToWorld;
                    tangentToWorld[0] = UnityObjectToWorldDir(v.tangent.xyz);
                    tangentToWorld[1] = cross(worldNormal, tangentToWorld[0]) * v.tangent.w;
                    tangentToWorld[2] = worldNormal;

                    // Inverse transpose to get world to tangent
                    float3x3 worldToTangent = transpose(tangentToWorld);

                    // Backlight direction (for rim/backlight effects)
                    o.lightDir = mul(worldToTangent, dominantLight);

                    // Calculate view direction and half vector
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                    // Half vector for specular (used in BRDF lookup)
                    float3 viewDirTS = mul(worldToTangent, viewDir);
                    o.halfVector = normalize(o.lightDir + viewDirTS);

                    UNITY_TRANSFER_FOG(o, o.pos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Sample textures
                    half4 diffuseSample = tex2D(_MainTex, i.uv);
                    half3 normalSample = tex2D(_BumpMap, i.uv).xyz;

                    // Unpack normal map
                    half3 normal = (normalSample * 2.0) - 1.0;
                    normal = normalize(normal);

                    // Calculate glossiness from alpha and specular strength vector
                    half glossiness = dot(_SpecularStrength, diffuseSample);

                    // === BRDF LOOKUP ===

                    // BRDF texture coordinates:
                    // X = dot(normal, lightDir) * 0.5 + 0.5  (maps -1..1 to 0..1)
                    // Y = dot(normal, halfVector)             (specular term)
                    half2 brdfUV;
                    brdfUV.x = (dot(normal, i.lightDir) * 0.5) + 0.5;
                    brdfUV.y = saturate(dot(normal, i.halfVector));

                    half4 brdfSample = tex2D(_BRDFTex, brdfUV);

                    // Combine BRDF response with glossiness
                    // RGB = diffuse response + specular response weighted by glossiness
                    // W = specular-only response
                    half3 brdfResponse = brdfSample.xyz + (glossiness * brdfSample.w);

                    // === FINAL COLOR ===

                    // Apply base color with BRDF-based lighting
                    half3 finalColor = diffuseSample.rgb * i.lighting.xyz;

                    // Add BRDF-based specular response (multiplied by 2 for visibility)
                    finalColor *= (brdfResponse * 2.0);

                    // Apply fog
                    UNITY_APPLY_FOG(i.fogCoord, finalColor);

                    return half4(finalColor, 1.0);
                }
                ENDCG
            }
        }

            Fallback "Mobile/Diffuse"
}