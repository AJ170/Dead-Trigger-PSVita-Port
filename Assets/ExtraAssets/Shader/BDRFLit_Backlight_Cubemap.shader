Shader "Vita/Character/BRDFLit Backlight - Cubemap" {
    Properties{
        _MainTex("Base (RGB) Specular (A)", 2D) = "grey" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        _SpecCubeTex("Specular Cubemap", CUBE) = "black" {}
        _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 1.0

        _LightProbesLightingAmount("Light Probes Lighting Amount", Range(0, 1)) = 0.9
    }

        SubShader{
            Tags { "RenderType" = "Opaque" "LIGHTMODE" = "ForwardBase" }
            LOD 300

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
                #pragma multi_compile_fog

                #define UNITY_SKINNED_MESH

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _BumpMap;
                samplerCUBE _SpecCubeTex;

                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                half _BumpScale;
                half4 _SpecColor;
                half _Roughness;
                half _SpecularIntensity;
                float _LightProbesLightingAmount;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float4 uv : TEXCOORD0;              // xy = main UV, zw = normal UV
                    half3 tSpace0 : TEXCOORD1;          // Tangent X
                    half3 tSpace1 : TEXCOORD2;          // Binormal X
                    half3 tSpace2 : TEXCOORD3;          // Normal X
                    half3 viewDirTS : TEXCOORD4;        // View direction in tangent space
                    half3 shLighting : TEXCOORD5;       // SH9 lighting + bias
                    UNITY_FOG_COORDS(6)
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

                    o.pos = UnityObjectToClipPos(v.vertex);

                    // Pack UVs
                    o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                    o.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);

                    // Build tangent-to-world matrix (rows)
                    half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                    half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

                    // Orthonormalize tangent
                    worldTangent = normalize(worldTangent - worldNormal * dot(worldNormal, worldTangent));

                    // Handedness
                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

                    // Store as rows
                    o.tSpace0 = half3(worldTangent.x, worldBinormal.x, worldNormal.x);
                    o.tSpace1 = half3(worldTangent.y, worldBinormal.y, worldNormal.y);
                    o.tSpace2 = half3(worldTangent.z, worldBinormal.z, worldNormal.z);

                    // View direction in tangent space
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    half3 worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
                    o.viewDirTS.x = dot(worldViewDir, worldTangent);
                    o.viewDirTS.y = dot(worldViewDir, worldBinormal);
                    o.viewDirTS.z = dot(worldViewDir, worldNormal);

                    // Sample spherical harmonics with bias
                    half3 sh = SampleSH9(worldNormal);
                    float lightingBias = 1.0 - _LightProbesLightingAmount;
                    o.shLighting = clamp(sh + lightingBias, 0.0, 1.0);

                    UNITY_TRANSFER_FOG(o, o.pos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Sample textures
                    half4 diffuseAlbedo = tex2D(_MainTex, i.uv.xy);

                    // Sample and unpack normal
                    half3 tangentNormal = UnpackNormal(tex2D(_BumpMap, i.uv.zw));
                    tangentNormal.xy *= _BumpScale;
                    tangentNormal = normalize(tangentNormal);

                    // Transform to world space using packed TBN
                    half3 worldNormal;
                    worldNormal.x = dot(i.tSpace0, tangentNormal);
                    worldNormal.y = dot(i.tSpace1, tangentNormal);
                    worldNormal.z = dot(i.tSpace2, tangentNormal);
                    worldNormal = normalize(worldNormal);

                    // Normalize view direction
                    half3 viewDirTS = normalize(i.viewDirTS);

                    // Transform view dir back to world space
                    half3 worldViewDir;
                    worldViewDir.x = dot(i.tSpace0, viewDirTS);
                    worldViewDir.y = dot(i.tSpace1, viewDirTS);
                    worldViewDir.z = dot(i.tSpace2, viewDirTS);

                    // === CUBEMAP SPECULAR ===

                    // Calculate reflection vector
                    half3 reflectionVector = reflect(-worldViewDir, worldNormal);

                    // Sample cubemap with roughness LOD
                    half roughnessLOD = _Roughness * 7.0;
                    half4 cubeReflection = texCUBElod(_SpecCubeTex, half4(reflectionVector, roughnessLOD));

                    // Fresnel effect (more reflection at glancing angles)
                    half NdotV = saturate(dot(worldNormal, worldViewDir));
                    half fresnel = pow(1.0 - NdotV, 2.0);

                    // Specular strength from diffuse alpha * property
                    half specularStrength = diffuseAlbedo.a * _SpecularIntensity;

                    // Combine specular with Fresnel
                    half3 specular = cubeReflection.rgb * _SpecColor.rgb * specularStrength * (0.2126 * diffuseAlbedo.r + 0.7152 * diffuseAlbedo.g + 0.0722 * diffuseAlbedo.b); // * fresnel

                    // === FINAL COLOR ===

                    // Base color with light probe lighting
                    half3 finalColor = diffuseAlbedo.rgb * i.shLighting;

                    // Add specular
                    finalColor += specular;

                    // Apply fog
                    UNITY_APPLY_FOG(i.fogCoord, finalColor);

                    return half4(finalColor, 1.0);
                }
                ENDCG
            }
        }

            Fallback "Mobile/Diffuse"
}