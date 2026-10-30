// Readable version of "MADFINGER/Characters/BRDFLit (Supports Backlight) - custom glossingess mask - zombie".
// Rewritten from the decompiled output. Behaviour is intended to match the original.
// Changes from the decompile: named variables, world-space tangent frame (equivalent for
// rigid transforms), SH evaluated with the built-in ShadeSH9, and the decompiler upgrade
// notes removed.

Shader "MADFINGER/Characters/BRDFLit (Supports Backlight) - custom glossingess mask - zombie (Readable)"
{
    Properties
    {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "grey" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
        _BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
        _LightProbesLightingAmount ("Light probes lighting amount", Range(0,1)) = 0.9
        _SpecularStrength ("Specular strength weights", Vector) = (0,0,0,1)
        _Params ("x = open holes, y = FPV projection", Vector) = (0,0,0,0)
    }

    SubShader
    {
        LOD 400
        Tags { "LightMode"="ForwardBase" "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" "RenderType"="Opaque" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _BRDFTex;

            float  _LightProbesLightingAmount;  // 1 = full probe lighting, 0 = none
            float4 _Params;                      // x = vertex-alpha hole threshold, y = FPV projection on/off
            float4 _SpecularStrength;            // weights that turn the base texture into a gloss value
            float4 _ProjParams;                  // engine projection parameters (used in FPV mode)

            uniform vector _ScreenTint;

            struct appdata_t
            {
                float4 vertex    : POSITION;
                float4 color     : COLOR;
                float3 normal    : NORMAL;
                float4 texcoord0 : TEXCOORD0;
                float4 tangent   : TANGENT;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 lightDirTS : TEXCOORD1; // dominant probe light direction, tangent space
                float3 halfDirTS  : TEXCOORD2; // half vector (view + light), tangent space
                float3 probeLight : COLOR0;    // light probe lighting for this vertex
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                // ---- Tangent frame (world space) ------------------------------------
                float3 worldNormal = mul((float3x3)unity_ObjectToWorld, normalize(v.normal));
                float3 worldTangent = normalize(mul((float3x3)unity_ObjectToWorld, normalize(v.tangent.xyz)));
                float3 worldBinormal = cross(normalize(worldNormal), worldTangent) * v.tangent.w;

                // Rows are tangent, binormal, normal: mul(tangentToWorld, dir) gives tangent-space dir.
                float3x3 worldToTangent = float3x3(worldTangent, worldBinormal, normalize(worldNormal));

                // ---- Clip-space position --------------------------------------------
                // In FPV mode, use the engine's projection parameters to adjust the projection.
                float4 projFix = float4(1.0, 1.0, 1.0, 0.0);
                if (_Params.y > 0.0)
                {
                    projFix = _ProjParams;
                }

                float4x4 proj = UNITY_MATRIX_P;
                proj._m00 *= projFix.x;
                proj._m11 *= projFix.y;

                // Vertices whose alpha is below the hole threshold collapse to the camera origin.
                float3 viewPos = UnityObjectToViewPos(v.vertex).xyz;
                if (v.color.a < _Params.x)
                {
                    viewPos = float3(0.0, 0.0, 0.0);
                }

                float4 clip = mul(proj, float4(viewPos, 1.0));
                o.pos = clip;
                o.pos.z = clip.z * projFix.z + projFix.w * clip.w;
                o.pos.z = max(o.pos.z, 0.0);

                // ---- Light probe lighting (Unity's built-in SH) ----------------------
                // ShadeSH9 uses the same L0/L1/L2 terms as the original hand-written code.
                float3 probeSH = ShadeSH9(float4(mul((float3x3)unity_ObjectToWorld, normalize(v.normal)), 1.0));
                o.probeLight = saturate(probeSH + (1.0 - _LightProbesLightingAmount));

                // ---- Dominant light direction from the probes -----------------------
                // Luminance-weighted sum of the L1 (linear) terms gives the main light direction.
                float3 dominantDir = unity_SHAr.xyz * 0.3
                                   + unity_SHAg.xyz * 0.59
                                   + unity_SHAb.xyz * 0.11;
                float3 lightDirWorld = normalize(dominantDir);
                o.lightDirTS = normalize(mul(worldToTangent, lightDirWorld));

                // ---- Half vector (view + light) in tangent space --------------------
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 viewDirWorld = normalize(_WorldSpaceCameraPos - worldPos);
                float3 viewDirTS = normalize(mul(worldToTangent, viewDirWorld));
                o.halfDirTS = normalize(viewDirTS + o.lightDirTS);

                o.uv = v.texcoord0.xy;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 baseTex = tex2D(_MainTex, i.uv);

                // Tangent-space normal (UnpackNormal handles the packed X-in-alpha case)
                float3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));

                // Gloss: weighted sum of the base texture channels
                float gloss = dot(_SpecularStrength, baseTex);

                // Diffuse: base colour times probe lighting
                float3 diffuse = baseTex.rgb * i.probeLight;

                // BRDF lookup: x = NdotL remapped to 0..1, y = NdotH
                float2 brdfUV;
                brdfUV.x = dot(normalTS, i.lightDirTS) * 0.5 + 0.5;
                brdfUV.y = dot(normalTS, i.halfDirTS);
                float4 brdf = tex2D(_BRDFTex, brdfUV);

                float3 specularTerm = (brdf.rgb + gloss * brdf.a) * 2.0;	//this is hardly accurate...

                float4 result;
                result.rgb = diffuse * specularTerm;// + glossboost;
                result.a = baseTex.a;
                return result + _ScreenTint;
            }
            ENDCG
        }
    }
}
