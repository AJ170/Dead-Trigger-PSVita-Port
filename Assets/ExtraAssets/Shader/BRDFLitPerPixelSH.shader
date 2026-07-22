Shader "Vita/Character/BRDFLit Backlight - Per Pixel SH" {
    Properties{
        _MainTex("Base (RGB) Gloss (A)", 2D) = "grey" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BRDFTex("BRDF Lookup (NdotL NdotH)", 2D) = "white" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        _SHIntensity("SH Lighting Intensity", Range(0, 2)) = 1.0
        _SHBias("SH Dark Floor Bias", Range(0, 1)) = 0.2
        _Ambient("Ambient Floor", Color) = (0.05, 0.05, 0.05, 1)

        _CubeTex("Reflection Cubemap", CUBE) = "black" {}
        _ReflectionStrength("Reflection Strength", Range(0, 3)) = 0.4
        _Roughness("Roughness", Range(0, 1)) = 0.1
        _TintColor("Tint Color", Color) = (1, 1, 1, 1)

        [HideInInspector] _SHAr("SH Ar", Vector) = (0,0,0,0)
        [HideInInspector] _SHAg("SH Ag", Vector) = (0,0,0,0)
        [HideInInspector] _SHAb("SH Ab", Vector) = (0,0,0,0)
        [HideInInspector] _SHBr("SH Br", Vector) = (0,0,0,0)
        [HideInInspector] _SHBg("SH Bg", Vector) = (0,0,0,0)
        [HideInInspector] _SHBb("SH Bb", Vector) = (0,0,0,0)
        [HideInInspector] _SHC("SH C",  Vector) = (0,0,0,0)
    }

    SubShader{
        Tags { "RenderType" = "Opaque" "LIGHTMODE" = "ForwardBase" }
        LOD 300

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma fragmentoption ARB_precision_hint_fastest

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D   _MainTex;
            sampler2D   _BumpMap;
            sampler2D   _BRDFTex;
            samplerCUBE _CubeTex;

            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            half   _BumpScale;
            half   _SHIntensity;
            half   _SHBias;
            half4  _Ambient;
            half   _ReflectionStrength;
            half   _Roughness;
            half4  _TintColor;

            float4 _SHAr;
            float4 _SHAg;
            float4 _SHAb;
            float4 _SHBr;
            float4 _SHBg;
            float4 _SHBb;
            float4 _SHC;

            struct appdata {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos          : SV_POSITION;
                float4 uv           : TEXCOORD0;
                half3  tSpace0      : TEXCOORD1;
                half3  tSpace1      : TEXCOORD2;
                half3  tSpace2      : TEXCOORD3;
                half3  lightDirTS   : TEXCOORD5;
                half3  viewDirTS    : TEXCOORD6;
                half3  worldViewDir : TEXCOORD7;
                UNITY_FOG_COORDS(4)
            };

            half3 EvaluateCustomSH(half3 worldNormal)
            {
                half3 n = worldNormal.xzy;
                float4 nv = float4(n, 1.0);

                half3 x1;
                x1.r = dot(_SHAr, nv);
                x1.g = dot(_SHAg, nv);
                x1.b = dot(_SHAb, nv);

                float4 vB = nv.xyzz * nv.yzzx;
                half3 x2;
                x2.r = dot(_SHBr, vB);
                x2.g = dot(_SHBg, vB);
                x2.b = dot(_SHBb, vB);

                float vC = n.x * n.x - n.y * n.y;
                half3 x3 = _SHC.rgb * vC;

                return x1 + x2 + x3;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);

                half3 worldNormal  = UnityObjectToWorldNormal(v.normal);
                half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

                worldTangent = normalize(
                    worldTangent - worldNormal
                    * dot(worldNormal, worldTangent));

                half tangentSign = v.tangent.w
                    * unity_WorldTransformParams.w;
                half3 worldBinormal = cross(worldNormal, worldTangent)
                    * tangentSign;

                // TBN rows for world normal reconstruction in fragment
                o.tSpace0 = half3(
                    worldTangent.x, worldBinormal.x, worldNormal.x);
                o.tSpace1 = half3(
                    worldTangent.y, worldBinormal.y, worldNormal.y);
                o.tSpace2 = half3(
                    worldTangent.z, worldBinormal.z, worldNormal.z);

                // TBN matrix for transforming light and view to tangent space
                half3x3 tbnMatrix;
                tbnMatrix[0] = half3(
                    worldTangent.x, worldTangent.y, worldTangent.z);
                tbnMatrix[1] = half3(
                    worldBinormal.x, worldBinormal.y, worldBinormal.z);
                tbnMatrix[2] = half3(
                    worldNormal.x, worldNormal.y, worldNormal.z);

                // Light direction in tangent space for BRDF NdotL
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                half3 worldLightDir = normalize(
                    UnityWorldSpaceLightDir(worldPos));
                o.lightDirTS = mul(tbnMatrix, worldLightDir);

                // World and tangent space view direction
                o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.viewDirTS    = mul(tbnMatrix, o.worldViewDir);

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Sample diffuse and gloss mask
                half4 diffuseAlbedo = tex2D(_MainTex, i.uv.xy);

                // Unpack and scale normal map
                half3 tangentNormal = UnpackNormal(
                    tex2D(_BumpMap, i.uv.zw));
                tangentNormal.xy *= _BumpScale;
                tangentNormal = normalize(tangentNormal);

                // Reconstruct world normal from TBN
                half3 worldNormal;
                worldNormal.x = dot(i.tSpace0, tangentNormal);
                worldNormal.y = dot(i.tSpace1, tangentNormal);
                worldNormal.z = dot(i.tSpace2, tangentNormal);
                worldNormal = normalize(worldNormal);

                // === SH AMBIENT ===
                half3 sh = EvaluateCustomSH(worldNormal);

                // Bias scaled by luminance so distant dark areas
                // don't get pushed further into black
                half shLum = dot(sh, half3(0.2126, 0.7152, 0.0722));
                half3 ambient = max(
                    sh * _SHIntensity - _SHBias * shLum,
                    _Ambient.rgb);

                // === BRDF DIRECTIONAL ===
                // NdotL and NdotH lookup into pre-baked BRDF texture
                // gives physically plausible diffuse and specular
                // response to the scene directional light cheaply
                half3 lightDirTS = normalize(i.lightDirTS);
                half3 viewDirTS  = normalize(i.viewDirTS);

                half2 brdfUV;
                brdfUV.x = dot(tangentNormal, lightDirTS) * 0.5 + 0.5;
                brdfUV.y = dot(tangentNormal,
                    normalize(lightDirTS + viewDirTS));

                half4 brdf = tex2D(_BRDFTex, brdfUV);

                // Diffuse plus gloss-weighted specular from BRDF lookup
                // x2 restores energy after 0-1 texture range
                half3 brdfLit = diffuseAlbedo.rgb
                              * (brdf.rgb + diffuseAlbedo.a * brdf.a)
                              * 2.0;

                // === CUBEMAP REFLECTION ===
                half3 worldViewDir     = normalize(i.worldViewDir);
                half3 reflectionVector = reflect(-worldViewDir, worldNormal);
                half  roughnessLOD     = _Roughness * 7.0
                    * clamp(1.0 - diffuseAlbedo.a, 0.0, 1.0);
                half3 cubeColor = texCUBElod(
                    _CubeTex,
                    half4(reflectionVector, roughnessLOD)).rgb;

                // Fresnel unwrapped to avoid pow()
                half NdotV     = saturate(dot(worldNormal, worldViewDir));
                half NdotV_inv = 1.0 - NdotV;
                half fresnel   = NdotV_inv * NdotV_inv;

                half reflAmount = diffuseAlbedo.a
                                * _ReflectionStrength
                                * fresnel;

                // === FINAL COMBINE ===
                // SH ambient gives probe-based environment colour
                // BRDF gives directional light and surface detail
                // Cubemap adds specular highlights
                half3 finalColor = brdfLit * ambient
                                 + cubeColor * reflAmount * _TintColor.rgb;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Mobile/Diffuse"
}
