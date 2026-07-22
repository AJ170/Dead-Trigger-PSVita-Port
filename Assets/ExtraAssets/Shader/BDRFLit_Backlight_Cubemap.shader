Shader "Vita/Character/BRDFLit Backlight - Per Pixel SH_Broken" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "grey" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0
        _SHIntensity("SH Lighting Intensity", Range(0, 2)) = 1.0
        _SHSubtract("SH Lighting Subtract", Range(0, 4)) = 4.0
        _Ambient("Ambient Floor", Color) = (0.05, 0.05, 0.05, 1)

        _CubeTex("Reflection Cubemap", CUBE) = "black" {}
        _ReflectionStrength("Reflection Strength", Range(0, 3)) = 0.4
        _WetGloss("Wet Gloss (sharpens reflection)", Range(0, 1)) = 0.7
        _Roughness("Roughness", Range(0, 1)) = 0.1
        _TintColor("Tint Color", Color) = (1, 1, 1, 1)


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

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BumpMap;

            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            half _BumpScale;
            half _SHIntensity;
            half _SHSubtract;
            half4 _Ambient;


            samplerCUBE _CubeTex;
            half   _ReflectionStrength;
            half   _WetGloss;
            half4  _TintColor;
            half _Roughness;

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
                float4 pos      : SV_POSITION;
                float4 uv       : TEXCOORD0; // xy = main, zw = bump
                half3 tSpace0   : TEXCOORD1; // TBN row 0
                half3 tSpace1   : TEXCOORD2; // TBN row 1
                half3 tSpace2   : TEXCOORD3; // TBN row 2
                half3 worldViewDir : TEXCOORD5;
                UNITY_FOG_COORDS(4)
            };

            // Evaluate SH using custom property block coefficients
            // Uses xzy swizzle to convert Unity world space to
            // the GL/SH coordinate convention the data was baked in
            half3 EvaluateCustomSH(half3 worldNormal)
            {
                // Coordinate convention fix
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
                    worldTangent - worldNormal
                    * dot(worldNormal, worldTangent));

                // Handedness
                half tangentSign = v.tangent.w
                    * unity_WorldTransformParams.w;
                half3 worldBinormal = cross(worldNormal, worldTangent)
                    * tangentSign;

                // Store TBN as rows for fragment shader reconstruction
                o.tSpace0 = half3(
                    worldTangent.x, worldBinormal.x, worldNormal.x);
                o.tSpace1 = half3(
                    worldTangent.y, worldBinormal.y, worldNormal.y);
                o.tSpace2 = half3(
                    worldTangent.z, worldBinormal.z, worldNormal.z);

                UNITY_TRANSFER_FOG(o, o.pos);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 diffuseAlbedo = tex2D(_MainTex, i.uv.xy);

                half3 tangentNormal = UnpackNormal(tex2D(_BumpMap, i.uv.zw));
                tangentNormal.xy *= _BumpScale;
                tangentNormal = normalize(tangentNormal);

                half3 worldNormal;
                worldNormal.x = dot(i.tSpace0, tangentNormal);
                worldNormal.y = dot(i.tSpace1, tangentNormal);
                worldNormal.z = dot(i.tSpace2, tangentNormal);
                worldNormal = normalize(worldNormal);

                half3 sh = EvaluateCustomSH(worldNormal);
                // Contrast curve — push darks down and brights up
                // Equivalent to a gentle power curve without calling pow()
                half shLum = dot(sh, half3(0.2126, 0.7152, 0.0722));
                half contrast = shLum * shLum; // Square darkens the response curve
                sh = sh * lerp(contrast, 1.0, _SHIntensity);


                // Find the brightest channel to use as a normalisation reference
                // This preserves colour ratios while allowing contrast control
                half shMax = max(sh.r, max(sh.g, sh.b));

                // Remap SH output so the brightest point maps to 1.0
                // This maximises contrast use across the probe range
                // rather than having everything compressed into 0.2-0.8
                half3 shNormalised = (shMax > 0.001h)
                    ? sh / shMax
                    : half3(1, 1, 1);

                // Lerp between normalised (full contrast) and raw (accurate colour)
                // _SHIntensity now controls contrast rather than brightness
                half3 shContrast = lerp(sh, shNormalised, _SHIntensity);

                // Clamp with ambient floor after contrast expansion
                // so darks don't go below minimum visibility
                half3 lighting = max(shContrast, _Ambient.rgb);

                half3 worldViewDir = normalize(i.worldViewDir);
                half3 reflectionVector = reflect(-worldViewDir, worldNormal);
                half roughnessLOD = _Roughness * 7.0
                    * clamp(1.0 - diffuseAlbedo.a, 0.0, 1.0);
                half3 cubeColor = texCUBElod(
                    _CubeTex, half4(reflectionVector, roughnessLOD)).rgb;

                half NdotV = saturate(dot(worldNormal, worldViewDir));
                half NdotV_inv = 1.0 - NdotV;
                half fresnel = NdotV_inv * NdotV_inv;

                half reflAmount = diffuseAlbedo.a * _ReflectionStrength * fresnel;

                half3 finalColor = diffuseAlbedo.rgb * lighting;
                    //+ cubeColor * reflAmount * _TintColor.rgb;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Mobile/Diffuse"
}
