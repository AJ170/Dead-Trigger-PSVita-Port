Shader "Vita/Character/BRDFLit Backlight - Cubemap_VertLit" {
    Properties{
        _MainTex("Base (RGB) Specular (A)", 2D) = "grey" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        _SpecCubeTex("Specular Cubemap", CUBE) = "black" {}
        _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 1.0

        _SHIntensity("SH Lighting Intensity", Range(0, 2)) = 0.5
        _Ambient("Ambient Floor", Color) = (0.05, 0.05, 0.05, 1)

        // SH coefficients injected by LightProbeSamplerDT
        // via MaterialPropertyBlock - not exposed in Inspector
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
            half _SHIntensity;
            half4 _Ambient;

            // Custom SH coefficients from LightProbeSamplerDT
            float4 _SHAr;
            float4 _SHAg;
            float4 _SHAb;
            float4 _SHBr;
            float4 _SHBg;
            float4 _SHBb;
            float4 _SHC;

            struct appdata {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos          : SV_POSITION;
                float4 uv           : TEXCOORD0; // xy = main, zw = bump
                half3 tSpace0       : TEXCOORD1; // TBN row 0
                half3 tSpace1       : TEXCOORD2; // TBN row 1
                half3 tSpace2       : TEXCOORD3; // TBN row 2
                half3 viewDirTS     : TEXCOORD4; // View dir in tangent space
                half3 shLighting    : TEXCOORD5; // SH result per vertex
                UNITY_FOG_COORDS(6)
            };

            // Evaluate SH using custom property block coefficients
            // Uses xzy swizzle to convert Unity world space to
            // the GL/SH coordinate convention the data was baked in
            half3 EvaluateCustomSH(half3 worldNormal)
            {
                // Coordinate convention fix — swap Y and Z
                // to match the space the SH data was baked in
                half3 n = worldNormal.xzy;

                float4 normalVec = float4(n, 1.0);

                // L0 + L1 linear terms
                half3 x1;
                x1.r = dot(_SHAr, normalVec);
                x1.g = dot(_SHAg, normalVec);
                x1.b = dot(_SHAb, normalVec);

                // L2 quadratic terms
                float4 vB = normalVec.xyzz * normalVec.yzzx;
                half3 x2;
                x2.r = dot(_SHBr, vB);
                x2.g = dot(_SHBg, vB);
                x2.b = dot(_SHBb, vB);

                // Final L2 quadratic
                float vC = n.x * n.x - n.y * n.y;
                half3 x3 = _SHC.rgb * vC;

                return x1 + x2 + x3;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                o.pos = UnityObjectToClipPos(v.vertex);

                // Pack UVs
                o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);

                // Build tangent-to-world matrix
                half3 worldNormal  = UnityObjectToWorldNormal(v.normal);
                half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

                // Orthonormalize tangent
                worldTangent = normalize(
                    worldTangent - worldNormal * dot(worldNormal, worldTangent));

                // Handedness
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

                // Store TBN as rows
                o.tSpace0 = half3(worldTangent.x, worldBinormal.x, worldNormal.x);
                o.tSpace1 = half3(worldTangent.y, worldBinormal.y, worldNormal.y);
                o.tSpace2 = half3(worldTangent.z, worldBinormal.z, worldNormal.z);

                // View direction in tangent space
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                half3 worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.viewDirTS.x = dot(worldViewDir, worldTangent);
                o.viewDirTS.y = dot(worldViewDir, worldBinormal);
                o.viewDirTS.z = dot(worldViewDir, worldNormal);

                // Evaluate SH per vertex using world normal
                // Characters use the geometry normal for SH rather than
                // the bump normal — this is correct and cheaper since
                // SH is low frequency lighting anyway
                half3 sh = EvaluateCustomSH(worldNormal);
                o.shLighting = max(sh * _SHIntensity, _Ambient.rgb);

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Sample diffuse + specular mask
                half4 diffuseAlbedo = tex2D(_MainTex, i.uv.xy);

                // Sample and unpack normal map
                half3 tangentNormal = UnpackNormal(tex2D(_BumpMap, i.uv.zw));
                tangentNormal.xy *= _BumpScale;
                tangentNormal = normalize(tangentNormal);

                // Transform bump normal to world space via TBN
                half3 worldNormal;
                worldNormal.x = dot(i.tSpace0, tangentNormal);
                worldNormal.y = dot(i.tSpace1, tangentNormal);
                worldNormal.z = dot(i.tSpace2, tangentNormal);
                worldNormal = normalize(worldNormal);

                // Reconstruct world view direction from tangent space
                half3 viewDirTS = normalize(i.viewDirTS);
                half3 worldViewDir;
                worldViewDir.x = dot(i.tSpace0, viewDirTS);
                worldViewDir.y = dot(i.tSpace1, viewDirTS);
                worldViewDir.z = dot(i.tSpace2, viewDirTS);

                // === CUBEMAP SPECULAR ===
                half3 reflectionVector = reflect(-worldViewDir, worldNormal);

                // Sample cubemap with roughness-based LOD
                half roughnessLOD = _Roughness * 7.0;
                half4 cubeReflection = texCUBElod(
                    _SpecCubeTex, half4(reflectionVector, roughnessLOD));

                // Fresnel — unwrapped for exponent 2 to avoid pow()
                half NdotV = saturate(dot(worldNormal, worldViewDir));
                half NdotV_inv = 1.0 - NdotV;
                half fresnel = NdotV_inv * NdotV_inv;

                // Specular strength from diffuse alpha
                // Luminance-weighted to avoid colour shift
                half specularStrength = diffuseAlbedo.a * _SpecularIntensity;
                half luminance = 0.2126 * diffuseAlbedo.r
                               + 0.7152 * diffuseAlbedo.g
                               + 0.0722 * diffuseAlbedo.b;

                half3 specular = cubeReflection.rgb
                               * _SpecColor.rgb
                               * specularStrength
                               * luminance;

                // === FINAL COLOUR ===
                // Diffuse modulated by SH lighting from property block
                half3 finalColor = diffuseAlbedo.rgb * i.shLighting;

                // Add specular on top
                finalColor += specular;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Mobile/Diffuse"
}
