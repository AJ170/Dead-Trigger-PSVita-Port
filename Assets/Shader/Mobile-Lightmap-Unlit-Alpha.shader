Shader "Vita/Environment/Unlit Transparent with Lightmap" {
    Properties{
        _MainTex("Base Texture (RGB)", 2D) = "white" {}
    }

        SubShader{
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "true" }
            LOD 100
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _MainTex_ST;

                struct appdata {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                };

                v2f vert(appdata v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.uv1 = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                    return o;
                }

                half4 frag(v2f i) : SV_Target {
                    // Sample base texture
                    half4 color = tex2D(_MainTex, i.uv);

                    // Sample and decode lightmap
                    half3 lightmap = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1));

                    // Apply lightmap with 2x multiplier
                    color.rgb *= (2.0 * lightmap);

                    return color;
                }
                ENDCG
            }
    }

        Fallback "Transparent/VertexLit"
}