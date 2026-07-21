Shader "Vita/Particles/Blood Smear - Multiply Cubemap" {
    Properties{
        _MainTex("Blood Texture (RGB) Alpha (A)", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _CubeTex("Reflection Cubemap", CUBE) = "black" {}

        _BumpScale("Normal Scale", Range(0, 2)) = 1.0
        _BumpTile("Normal Tiling", Range(0.1, 8)) = 1.0
        _ReflectionStrength("Reflection Strength", Range(0, 3)) = 0.4
        _WetGloss("Wet Gloss (sharpens reflection)", Range(0, 1)) = 0.7
        _Color("Tint Color", Color) = (1, 1, 1, 1)
    }

        SubShader{
            Tags {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
            }

            LOD 100
            Lighting Off
            ZWrite Off
            Blend DstColor Zero

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma fragmentoption ARB_precision_hint_fastest

                #include "UnityCG.cginc"

                sampler2D   _MainTex;
                sampler2D   _BumpMap;
                samplerCUBE _CubeTex;

                float4 _MainTex_ST;
                half   _BumpScale;
                half   _BumpTile;
                half   _ReflectionStrength;
                half   _WetGloss;
                half4  _Color;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv     : TEXCOORD0;
                    float3 normal : NORMAL;
                    half4  color  : COLOR;
                };

                struct v2f
                {
                    float4 pos      : SV_POSITION;
                    float2 uv       : TEXCOORD0;
                    float2 bumpUV   : TEXCOORD1;

                    // Full TBN packed as rows so the fragment shader
                    // can correctly transform the normal map into
                    // world space regardless of surface orientation
                    half3  tSpace0  : TEXCOORD2;
                    half3  tSpace1  : TEXCOORD3;
                    half3  tSpace2  : TEXCOORD4;

                    half3  viewDir  : TEXCOORD5;
                    half4  color    : COLOR;
                };

                v2f vert(appdata_t v)
                {
                    v2f o;

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.bumpUV = v.uv * _BumpTile;

                    // World space normal and view direction
                    half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    half3 worldViewDir = normalize(
                        _WorldSpaceCameraPos - worldPos);

                    // Derive tangent frame from surface normal and view dir
                    // Particles carry no tangent data so we construct one
                    // that adapts to the actual surface orientation —
                    // correct on floors, walls and any angled surface
                    half3 worldTangent = normalize(
                        cross(worldNormal, worldViewDir));

                    // Degenerate fallback: normal and view dir are parallel
                    // (camera looking straight down at a horizontal surface)
                    if (length(worldTangent) < 0.01h)
                        worldTangent = normalize(
                            cross(worldNormal, half3(0, 1, 0)));

                    // Binormal completes the right-handed frame
                    half3 worldBinormal = cross(worldNormal, worldTangent);

                    // Store TBN as rows for fragment dot products
                    o.tSpace0 = half3(
                        worldTangent.x, worldBinormal.x, worldNormal.x);
                    o.tSpace1 = half3(
                        worldTangent.y, worldBinormal.y, worldNormal.y);
                    o.tSpace2 = half3(
                        worldTangent.z, worldBinormal.z, worldNormal.z);

                    o.viewDir = worldViewDir;
                    o.color = v.color * _Color;

                    return o;
                }

                half4 frag(v2f i) : COLOR
                {
                    // Sample textures
                    half4 mainTex = tex2D(_MainTex, i.uv);

                    // Unpack and scale normal map
                    half3 tangentNormal = UnpackNormal(
                        tex2D(_BumpMap, i.bumpUV));
                    tangentNormal.xy *= _BumpScale;
                    tangentNormal = normalize(tangentNormal);

                    // Transform normal map from tangent space to world space
                    // using the view-derived TBN — correct on all surfaces
                    half3 worldNormal;
                    worldNormal.x = dot(i.tSpace0, tangentNormal);
                    worldNormal.y = dot(i.tSpace1, tangentNormal);
                    worldNormal.z = dot(i.tSpace2, tangentNormal);
                    worldNormal = normalize(worldNormal);

                    // Reflection from correctly transformed world normal
                    half3 viewDir = normalize(i.viewDir);
                    half3 reflVec = reflect(-viewDir, worldNormal);

                    // Negate X for Unity left-handed to cubemap convention
                    reflVec.x = -reflVec.x;

                    // Sample cubemap
                    half3 cubeColor = texCUBE(_CubeTex, reflVec).rgb;

                    // Gloss sharpening — pow(gloss, 4) via two multiplies
                    half gloss = mainTex.a * _WetGloss;
                    half gloss2 = gloss * gloss;
                    half glossSharp = gloss2 * gloss2;
                    half reflAmount = glossSharp * _ReflectionStrength;

                    // Alpha drives multiply blend fadeout at edges
                    half finalAlpha = mainTex.a * i.color.a;
                    half3 bloodColor = mainTex.rgb * i.color.rgb;

                    // Lerp to white at edges for clean multiply blend
                    // then add reflection on top weighted by gloss and alpha
                    half3 blendColor = lerp(half3(1, 1, 1),
                        bloodColor, finalAlpha);
                    blendColor += cubeColor * reflAmount * mainTex.a;

                    return half4(blendColor, 1.0);
                }
                ENDCG
            }
        }

            Fallback "Mobile/Particles/Multiply"
}
