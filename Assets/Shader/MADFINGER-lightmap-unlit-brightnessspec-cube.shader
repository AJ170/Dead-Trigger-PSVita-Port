Shader "Vita/Environment/Cubemap Specular Optimized" {
    Properties{
        _MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
        _SpecCubeTex("SpecCube", CUBE) = "black" {}
        _SpecularStrength("Specular Strength", Range(0, 2)) = 1.0
        _Roughness("Roughness", Range(0, 1)) = 0.1
        _ScrollingSpeed("Scrolling speed", Vector) = (0, 0, 0, 0)
    }

        SubShader{
            LOD 100
            Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 3.0

                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                float4 _ScrollingSpeed;
                float4 _MainTex_ST;
                sampler2D _MainTex;
                samplerCUBE _SpecCubeTex;
                half _SpecularStrength;
                half _Roughness;

                uniform vector _ScreenTint;

                struct appdata_t {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    float4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    float4 color : COLOR;
                    float3 reflectionVector : TEXCOORD2;
                    half fresnel : TEXCOORD3;
                    UNITY_FOG_COORDS(4)
                };

                v2f vert(appdata_t v) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);

                    o.pos = UnityObjectToClipPos(v.vertex);

                    // Scrolling UVs
                    o.uv = (v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
                    o.uv += frac(_ScrollingSpeed.xy * _Time.y);

                    // Lightmap UVs
                    o.uv1 = (v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw;

                    // Vertex color
                    o.color = v.color;

                    // Reflection vector and fresnel per vertex
                    half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    half3 worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
                    // Not normalized: stays linear across a flat polygon, so it doesn't stretch
                    o.reflectionVector = reflect(worldPos - _WorldSpaceCameraPos, worldNormal);

                    half fresnelBase = 1.0 - saturate(dot(worldNormal, worldViewDir));
                    o.fresnel = fresnelBase * fresnelBase;

                    UNITY_TRANSFER_FOG(o, o.pos);

                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Sample base texture (1 sample)
                    half4 baseColor = tex2D(_MainTex, i.uv);

                    // Sample lightmap (1 sample)
                    half3 lightmap = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1));
#if defined(SHADER_API_PSP2)
    lightmap *= 0.4;
#endif

                    // Combine base with lightmap
                    half3 color = baseColor.rgb * (1.0 * lightmap) * i.color.rgb;

                    // === CUBEMAP REFLECTION ===

                    // Sample cubemap with roughness LOD (1 sample)
                    half roughnessLOD = _Roughness * 7.0;
                    half3 cubeColor = texCUBElod(_SpecCubeTex, float4(i.reflectionVector, roughnessLOD)).rgb;

                    // Use alpha as primary reflection strength
                    // Multiply by Fresnel and user property
                    half reflectionAmount = baseColor.a * i.fresnel * _SpecularStrength;
#if defined(SHADER_API_PSP2)
    reflectionAmount *= 0.4;
#endif
                    
                    // Add reflection to color (multiplicative blend is cheaper than lerp)
                    color += cubeColor * reflectionAmount;

                    UNITY_APPLY_FOG(i.fogCoord, color);

                    return half4(color, baseColor.a) + _ScreenTint;
                }
                ENDCG
            }
        }
}