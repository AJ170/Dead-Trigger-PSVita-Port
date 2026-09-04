Shader "Vita/Character/Cube env map Normal (Supports LightProbes) FPV"
{
    Properties{
        _MainTex("Diffuse (RGB)", 2D) = "grey" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        _CubeTex("Specular Cubemap", CUBE) = "black" {}
        _SpecularStrength("Specular Strength", Range(0, 2)) = 0.6
        _SpecularBase("Specular Base", Range(0, 1)) = 0.05
        _SpecularSharpness("Specular Sharpness", Range(1, 8)) = 3.0

            //_SHIntensity("SH Intensity", Range(0, 2)) = 1.0
            //_SHColorAmount("SH Color Amount", Range(0, 1)) = 0.6
            //_SHBias("SH Bias", Range(0, 1)) = 0.15
            _Ambient("Ambient Floor", Color) = (0.05, 0.05, 0.05, 1)

            [HideInInspector] _SHAr("SH Ar", Vector) = (0,0,0,0)
            [HideInInspector] _SHAg("SH Ag", Vector) = (0,0,0,0)
            [HideInInspector] _SHAb("SH Ab", Vector) = (0,0,0,0)
            [HideInInspector] _SHBr("SH Br", Vector) = (0,0,0,0)
            [HideInInspector] _SHBg("SH Bg", Vector) = (0,0,0,0)
            [HideInInspector] _SHBb("SH Bb", Vector) = (0,0,0,0)
            [HideInInspector] _SHC("SH C",  Vector) = (0,0,0,0)
    }

        SubShader{
            Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
            LOD 200

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 3.0
                #pragma fragmentoption ARB_precision_hint_fastest
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                sampler2D   _MainTex;
                sampler2D   _BumpMap;
                samplerCUBE _CubeTex;

                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                half   _BumpScale;
                half   _SpecularStrength;
                half   _SpecularBase;
                half   _SpecularSharpness;
                //half   _SHIntensity;
                //half   _SHBias;
                half4  _Ambient;
                //half _SHColorAmount;

                float4 _SHAr;
                float4 _SHAg;
                float4 _SHAb;
                float4 _SHBr;
                float4 _SHBg;
                float4 _SHBb;
                float4 _SHC;

                uniform vector _ScreenTint;

                struct appdata {
                    float4 vertex  : POSITION;
                    float3 normal  : NORMAL;
                    float4 tangent : TANGENT;
                    float2 uv      : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos          : SV_POSITION;
                    float4 uv           : TEXCOORD0; // xy = main, zw = bump
                    half3  tSpace0      : TEXCOORD1; // TBN row 0
                    half3  tSpace1      : TEXCOORD2; // TBN row 1
                    half3  tSpace2      : TEXCOORD3; // TBN row 2
                    half3  worldViewDir : TEXCOORD4;
                    UNITY_FOG_COORDS(5)
                };

                // Evaluate SH per pixel using bump normal
                // xzy swizzle corrects Unity to GL/SH coordinate convention
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

                    // Build TBN for fragment normal reconstruction
                    half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                    half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

                    worldTangent = normalize(
                        worldTangent - worldNormal
                        * dot(worldNormal, worldTangent));

                    half tangentSign = v.tangent.w
                        * unity_WorldTransformParams.w;
                    half3 worldBinormal = cross(worldNormal, worldTangent)
                        * tangentSign;

                    o.tSpace0 = half3(
                        worldTangent.x, worldBinormal.x, worldNormal.x);
                    o.tSpace1 = half3(
                        worldTangent.y, worldBinormal.y, worldNormal.y);
                    o.tSpace2 = half3(
                        worldTangent.z, worldBinormal.z, worldNormal.z);

                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.worldViewDir = normalize(
                        _WorldSpaceCameraPos - worldPos);

                    UNITY_TRANSFER_FOG(o, o.pos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    // Sample diffuse
                    half4 diffuse = tex2D(_MainTex, i.uv.xy);

                    // Unpack and scale bump normal
                    half3 tangentNormal = UnpackNormal(
                        tex2D(_BumpMap, i.uv.zw));
                    tangentNormal.xy *= _BumpScale;
                    tangentNormal = normalize(tangentNormal);

                    // Transform bump normal to world space via TBN
                    // This is what breaks the flat tube look —
                    // every pixel gets its own normal direction
                    half3 worldNormal;
                    worldNormal.x = dot(i.tSpace0, tangentNormal);
                    worldNormal.y = dot(i.tSpace1, tangentNormal);
                    worldNormal.z = dot(i.tSpace2, tangentNormal);
                    worldNormal = normalize(worldNormal);

                    half3 worldViewDir = normalize(i.worldViewDir);

                    // === SH LIGHTING — per pixel using bump normal ===
                    // Instead of multiplying diffuse directly by the full SH colour
                    // extract the luminance and lerp between coloured and neutral lighting
                    half3 sh = EvaluateCustomSH(worldNormal);
                    /*
                    half shLum = dot(sh, half3(0.2126, 0.7152, 0.0722));
                    half3 shNeutral = half3(shLum, shLum, shLum);

                    // _SHColorAmount controls how much colour the probes contribute
                    // 0 = pure luminance only, 1 = full probe colour
                    half3 shMixed = lerp(shNeutral, sh, _SHColorAmount);
                    half3 lighting = max(
                        shMixed * _SHIntensity - _SHBias * shLum,
                        _Ambient.rgb);
                        */

                    half3 lighting = max(sh, _Ambient.rgb);
                    // === CUBEMAP SPECULAR — bump normal for surface detail ===
                    half3 reflVec = reflect(-worldViewDir, worldNormal);
                    reflVec.x = -reflVec.x;

                    half3 cubeColor = texCUBE(_CubeTex, reflVec).rgb;

                    // Variable sharpness via lerp between NdotR and NdotR^4
                    // avoids pow() — low sharpness = broad, high = tight
                    half NdotR = saturate(dot(worldNormal, normalize(reflVec)));
                    half spec2 = NdotR * NdotR;
                    half spec4 = spec2 * spec2;
                    half spec = lerp(NdotR, spec4, _SpecularSharpness / 8.0);

                    half3 specular = saturate(_SpecularBase + cubeColor * spec * _SpecularStrength * diffuse.a);

                    // === FINAL ===
                    half3 finalColor = diffuse.rgb * lighting + specular;

                    UNITY_APPLY_FOG(i.fogCoord, finalColor);

                    return half4(finalColor, 1.0) + _ScreenTint;
                }
                ENDCG
            }
        }

            Fallback "Mobile/Diffuse"
}
