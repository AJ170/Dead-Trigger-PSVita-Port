Shader "MADFINGER/Characters/BRDFLit FX (Supports Backlight)" {
    Properties{
       _MainTex("Base (RGB) Gloss (A)", 2D) = "grey" {}
       _BumpMap("Normal Map", 2D) = "bump" {}
       _BRDFTex("NdotL NdotH (RGB)", 2D) = "white" {}
       _NoiseTex("Noise Tex", 2D) = "white" {}
       _FXColor("FX Color", Color) = (0, 0.97, 0.89, 1)
       _TimeOffs("Time Offset", Float) = 0
       _Duration("Duration", Float) = 2
       _Invert("Invert", Float) = 0
       _LightProbesLightingAmount("Light Probes Lighting Amount", Range(0, 1)) = 0.9

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
            LOD 400
            Tags {
                "QUEUE" = "Transparent"
                "IGNOREPROJECTOR" = "true"
                "RenderType" = "Transparent"
            }

            Pass {
                Name "FORWARD"
                Tags {
                    "LIGHTMODE" = "ForwardBase"
                    "QUEUE" = "Transparent"
                    "IGNOREPROJECTOR" = "true"
                    "RenderType" = "Transparent"
                }

                Blend SrcAlpha OneMinusSrcAlpha

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma fragmentoption ARB_precision_hint_fastest

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _BumpMap;
                sampler2D _BRDFTex;
                sampler2D _NoiseTex;

                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                half4  _FXColor;

                float  _TimeOffs;
                float  _Duration;
                float  _Invert;
                float  _GlobalTime;
                half   _LightProbesLightingAmount;

                // Custom SH coefficients from LightProbeSamplerDT
                float4 _SHAr;
                float4 _SHAg;
                float4 _SHAb;
                float4 _SHBr;
                float4 _SHBg;
                float4 _SHBb;
                float4 _SHC;

                struct appdata_t
                {
                    float4 vertex  : POSITION;
                    float2 uv      : TEXCOORD0;
                    float4 tangent : TANGENT;
                    float3 normal  : NORMAL;
                };

                struct v2f
                {
                    float4 pos          : SV_POSITION;
                    float4 uv           : TEXCOORD0; // xy = main, zw = bump
                    half3  shLighting   : TEXCOORD1; // SH result + ambient bias
                    half   fxProgress   : TEXCOORD2; // FX animation progress
                    half3  lightDirTS   : TEXCOORD3; // Light dir in tangent space
                    half3  viewDirTS    : TEXCOORD4; // View dir in tangent space
                };

                // Evaluate SH using custom property block coefficients
                // xzy swizzle converts Unity world space to GL/SH convention
                half3 EvaluateCustomSH(half3 worldNormal)
                {
                    half3 n = worldNormal.xzy;
                    float4 nv = float4(n, 1.0);

                    // L0 + L1 linear terms
                    half3 x1;
                    x1.r = dot(_SHAr, nv);
                    x1.g = dot(_SHAg, nv);
                    x1.b = dot(_SHAb, nv);

                    // L2 quadratic terms
                    float4 vB = nv.xyzz * nv.yzzx;
                    half3 x2;
                    x2.r = dot(_SHBr, vB);
                    x2.g = dot(_SHBg, vB);
                    x2.b = dot(_SHBb, vB);

                    // Final L2 quadratic
                    float vC = n.x * n.x - n.y * n.y;
                    half3 x3 = _SHC.rgb * vC;

                    return x1 + x2 + x3;
                }

                v2f vert(appdata_t v)
                {
                    v2f o;

                    o.pos = UnityObjectToClipPos(v.vertex);

                    // Pack UVs
                    o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                    o.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);

                    // World space normal
                    half3 worldNormal = UnityObjectToWorldNormal(v.normal);

                    // Build tangent-to-object matrix
                    // Used to transform light and view dirs into tangent space
                    half3 normal = normalize(v.normal);
                    half3 tangent = normalize(v.tangent.xyz);
                    half3 binormal = (cross(normal.yzx, tangent.zxy)
                                    - cross(normal.zxy, tangent.yzx))
                                    * v.tangent.w;

                    half3x3 tbn;
                    tbn[0] = half3(tangent.x,  binormal.x, normal.x);
                    tbn[1] = half3(tangent.y,  binormal.y, normal.y);
                    tbn[2] = half3(tangent.z,  binormal.z, normal.z);

                    // Light direction in tangent space
                    o.lightDirTS = mul(tbn,
                        mul((float3x3)unity_WorldToObject,
                            _WorldSpaceLightPos0.xyz));

                    // View direction in tangent space
                    float4 worldCamPos = float4(_WorldSpaceCameraPos, 1.0);
                    half3 objViewDir = normalize(
                        mul((float3x3)unity_WorldToObject, worldCamPos.xyz)
                        - v.vertex.xyz);
                    o.viewDirTS = mul(tbn, objViewDir);

                    // SH lighting with ambient bias
                    half3 sh = EvaluateCustomSH(worldNormal);
                    o.shLighting = saturate(
                        sh + (1.0 - _LightProbesLightingAmount));

                    // FX animation progress — time offset into duration
                    // clamped 0-1, optionally inverted
                    float progress = saturate(
                        (_TimeOffs + _GlobalTime) / _Duration);
                    o.fxProgress = (_Invert > 0.0)
                        ? (1.0 - progress)
                        : progress;

                    return o;
                }

                half4 frag(v2f i) : COLOR
                {
                    // Sample textures
                    half4 mainTex = tex2D(_MainTex,  i.uv.xy);
                    half4 noiseTex = tex2D(_NoiseTex, i.uv.xy * 2.0);

                    // Unpack normal map manually — cheaper than UnpackNormal
                    // on Vita since we avoid the * 2 - 1 on unused channel
                    half3 tangentNormal = tex2D(_BumpMap, i.uv.zw).xyz
                                        * 2.0 - 1.0;

                    // BRDF lookup using NdotL and NdotH
                    half2 brdfUV;
                    brdfUV.x = dot(tangentNormal, i.lightDirTS) * 0.5 + 0.5;
                    brdfUV.y = dot(tangentNormal,
                        normalize(i.lightDirTS + i.viewDirTS));

                    half4 brdf = tex2D(_BRDFTex, brdfUV);

                    // Diffuse modulated by SH lighting
                    half3 diffuse = mainTex.rgb * i.shLighting;

                    // BRDF lighting — diffuse term plus gloss-weighted specular
                    half3 brdfLit = diffuse
                                  * (brdf.rgb + (mainTex.a * brdf.a))
                                  * 2.0;

                    // FX dissolve edge — noise threshold alpha
                    half dissolveEdge = 1.0 - saturate(
                        (noiseTex.x - i.fxProgress) * 4.0);
                    half dissolveAlpha = float(noiseTex.x > i.fxProgress);

                    // FX glow — squared twice for tight edge highlight
                    half edgeGlow = dissolveEdge * dissolveEdge;
                    edgeGlow = edgeGlow * edgeGlow;

                    // Final colour
                    half3 finalColor = brdfLit
                                     + (_FXColor.rgb * edgeGlow);

                    return half4(finalColor, dissolveAlpha);
                }
                ENDCG
            }
       }

           Fallback "Mobile/Diffuse"
}
