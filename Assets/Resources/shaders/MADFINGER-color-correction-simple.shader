Shader "MADFINGER/PostFX/ColorCorrectionSimple" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "" {}
    }
        SubShader{
            Pass {
                ZTest Always
                ZWrite Off
                Cull Off
                Fog { Mode Off }
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest

                sampler2D _MainTex;
                half4 _ColorBias;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    half2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                };

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv.xy;
                    return o;
                }

                half4 frag(v2f i) : COLOR
                {
                    return tex2D(_MainTex, i.uv) + _ColorBias;
                }
                ENDCG
            }
    }
}