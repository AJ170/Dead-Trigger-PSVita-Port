Shader "Vita/Character/WeaponSurfaceSpec" {
    Properties{
        _MainTex("Diffuse (RGB) Specular (A)", 2D) = "white" {}
        //_BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        _SpecCubeTex("Specular Cubemap", CUBE) = "black" {}
        _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 1.0

        _LightProbesLightingAmount("Light Probes Lighting Amount", Range(0, 1)) = 0.9
    }

        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 200

            Pass {
                Tags { "LightMode" = "ForwardBase" }

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #pragma multi_compile_fwdbase
                #pragma multi_compile_fog

                #define UNITY_SKINNED_MESH

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                //sampler2D _BumpMap;
                samplerCUBE _SpecCubeTex;

                float4 _MainTex_ST;
                //float4 _BumpMap_ST;
                half _BumpScale;
                half4 _SpecColor;
                half _Roughness;
                half _SpecularIntensity;
                half _LightProbesLightingAmount;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float4 uv : TEXCOORD0;
                    half3 worldNormal : TEXCOORD1;
                    half3 worldViewDir : TEXCOORD2;
                    half3 shLighting : TEXCOORD3;
                    UNITY_FOG_COORDS(6)
                };

                v2f vert(appdata v) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    o.pos = UnityObjectToClipPos(v.vertex);

                    o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                    //o.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);

                    o.worldNormal = UnityObjectToWorldNormal(v.normal);

                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
                    half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

                    worldTangent = normalize(worldTangent - o.worldNormal * dot(o.worldNormal, worldTangent));

                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 worldBinormal = cross(o.worldNormal, worldTangent) * tangentSign;

                    half3 tSpace0 = half3(worldTangent.x, worldBinormal.x, o.worldNormal.x);
                    half3 tSpace1 = half3(worldTangent.y, worldBinormal.y, o.worldNormal.y);
                    half3 tSpace2 = half3(worldTangent.z, worldBinormal.z, o.worldNormal.z);

                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
                    //o.viewDirTS.x = dot(worldViewDir, worldTangent);
                    //o.viewDirTS.y = dot(worldViewDir, worldBinormal);
                    //o.viewDirTS.z = dot(worldViewDir, o.worldNormal);

                    // Sample spherical harmonics with proper bias
                    half3 sh = ShadeSH9(half4(o.worldNormal, 1.0));

                    // Apply the lighting amount bias (Madfinger approach)
                    // This ensures minimum lighting when _LightProbesLightingAmount is high
                    o.shLighting = sh + (1.0 - _LightProbesLightingAmount);

                    UNITY_TRANSFER_FOG(o, o.pos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    half4 diffuseAlbedo = tex2D(_MainTex, i.uv.xy);

                    // Clamp lighting to valid range
                    half3 lighting = clamp(i.shLighting, 0.0, 1.0);

                    // === SPECULAR FROM CUBEMAP ===

                    half3 reflectionVector = reflect(-i.worldViewDir, i.worldNormal);
                    half roughnessLOD = _Roughness * 7.0;
                    half4 cubeReflection = texCUBElod(_SpecCubeTex, half4(reflectionVector, roughnessLOD));

                    //half NdotV = saturate(dot(worldNormal, worldViewDir));
                    //half fresnel = pow(1.0 - NdotV, 2.0);

                    //half specularStrength = diffuseAlbedo.a * _SpecularIntensity; //Dropped this as I felt it was more effective to have this surface absurdly shiny
                    half3 specular = cubeReflection.rgb * _SpecColor.rgb * _SpecularIntensity * (0.2126 * diffuseAlbedo.r + 0.7152 * diffuseAlbedo.g + 0.0722 * diffuseAlbedo.b);

                    // === FINAL COLOR ===

                    half3 finalColor = diffuseAlbedo.rgb * lighting;
                    finalColor += specular;

                    UNITY_APPLY_FOG(i.fogCoord, finalColor);

                    return half4(finalColor, 1.0);
                }
                ENDCG
            }
        }

            Fallback "Mobile/Diffuse"
}