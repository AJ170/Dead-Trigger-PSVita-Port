Shader "Vita/Environment/Sparkling Snow" {
    Properties{
        _MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
        _NormalTex("Normal Map", 2D) = "bump" {}
        _SpecCubeTex("SpecCube", CUBE) = "black" {}
        _SpecularStrength("Specular Strength", Range(0, 2)) = 1.0
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _SparkleIntensity("Sparkle Intensity", Range(0, 5)) = 2.0
        _SparkleSharpness("Sparkle Sharpness", Range(1, 16)) = 8.0
        _ScrollingSpeed("Scrolling speed", Vector) = (0,0,0,0)
    }
        SubShader{
            LOD 100
            Tags { "LIGHTMODE" = "ForwardBase" "RenderType" = "Opaque" }
            Pass {
                Tags { "LIGHTMODE" = "ForwardBase" "RenderType" = "Opaque" }
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                float4 _ScrollingSpeed;
                float4 _MainTex_ST;
                float4 _NormalTex_ST;
                sampler2D _MainTex;
                sampler2D _NormalTex;
                samplerCUBE _SpecCubeTex;
                half _SpecularStrength;
                half _Roughness;
                half _SparkleIntensity;
                half _SparkleSharpness;

                struct appdata_t {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    float4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    float2 uvNormal : TEXCOORD2;
                    float4 color : COLOR;
                    half3 worldNormal : TEXCOORD3;
                    half3 worldTangent : TEXCOORD4;
                    half3 worldBinormal : TEXCOORD5;
                    half3 worldViewDir : TEXCOORD6;
                    UNITY_FOG_COORDS(7)
                };

                v2f vert(appdata_t v) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    o.pos = UnityObjectToClipPos(v.vertex);

                    // Scrolling UVs for main texture
                    o.uv = (v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
                    o.uv += frac(_ScrollingSpeed.xy * _Time.y);

                    // Lightmap UVs
                    o.uv1 = (v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw;

                    // Normal texture UVs with proper tiling
                    o.uvNormal = (v.uv.xy * _NormalTex_ST.xy) + _NormalTex_ST.zw;

                    // Vertex color
                    o.color = v.color;

                    // World space normal
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);

                    // World space tangent space (for normal map)
                    o.worldTangent = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)));
                    o.worldBinormal = normalize(cross(o.worldNormal, o.worldTangent) * v.tangent.w);

                    // World space view direction
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.worldViewDir = _WorldSpaceCameraPos - worldPos;

                    // Transfer fog coordinates
                    UNITY_TRANSFER_FOG(o, o.pos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Sample base texture
                    half4 baseColor = tex2D(_MainTex, i.uv);

                    // Decode lightmap
                    half3 lightmap = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1));

                    // Apply lightmap to base color
                    half3 color = baseColor.rgb * (1.0 * lightmap);

                    // Apply vertex color modulation
                    color *= i.color.rgb;

                    // === DUAL NORMAL MAP SAMPLING FOR GLITTER ===
                    
                    // Sample 1: Texture-space normal map (how snow is draped on the surface)
                    half3 normalTexture = tex2D(_NormalTex, i.uvNormal).rgb;
                    normalTexture = normalTexture * 2.0 - 1.0;  // Unpack from [0,1] to [-1,1]

                    // Sample 2: Screen-space normal map (creates glitter variation as camera moves)
                    // Scale screen coordinates to control glitter frequency
                    float2 screenSpaceNormal = i.pos.xy/i.pos.w;
                    half3 normalScreen = tex2D(_NormalTex, screenSpaceNormal).rgb;
                    normalScreen = normalScreen * 2.0 - 1.0;  // Unpack from [0,1] to [-1,1]

                    // Multiply the two normal samples together for combined effect
                    half3 combinedNormal = normalTexture * normalScreen;

                    // Convert combined normal from tangent space to world space
                    half3 worldNormal = normalize(i.worldNormal);
                    half3 glitterNormal = normalize(
                        combinedNormal.x * normalize(i.worldTangent) +
                        combinedNormal.y * normalize(i.worldBinormal) +
                        combinedNormal.z * worldNormal
                    );

                    // === CUBEMAP REFLECTION ===
                    half3 worldViewDir = normalize(i.worldViewDir);

                    // Calculate reflection vector using glitter normal
                    half3 reflectionVector = reflect(-worldViewDir, glitterNormal);

                    // Sample cubemap with roughness LOD
                    half roughnessLOD = _Roughness * 7.0;
                    half4 cubeColor = texCUBElod(_SpecCubeTex, half4(reflectionVector, roughnessLOD));

                    // === GLITTER SPARKLE EFFECT ===
                    // Check how well the combined glitter normal aligns with view direction
                    half NdotV_Glitter = saturate(dot(glitterNormal, worldViewDir));
                    
                    // Create sharp sparkle peaks - high exponent = tight sparkles
                    half sparkle = pow(NdotV_Glitter, _SparkleSharpness);
                    
                    // Apply sparkle intensity and mask by base alpha (gloss map)
                    sparkle *= _SparkleIntensity * baseColor.a;

                    // === FRESNEL REFLECTION ===
                    half NdotV = saturate(dot(worldNormal, worldViewDir));
                    half fresnel = (1.0 - NdotV) * (1.0 - NdotV);
                    
                    half reflectionAmount = baseColor.a * _SpecularStrength * fresnel;

                    // Combine cubemap reflection with sparkle
                    // Sparkles are bright points added on top
                    color = lerp(color, color + cubeColor.rgb, reflectionAmount);
                    color += cubeColor.rgb * sparkle * 0.5;  // Sparkle contribution

                    // Apply fog
                    UNITY_APPLY_FOG(i.fogCoord, color);

                    return half4(color, baseColor.a);
                }
                ENDCG
            }
        }
}
