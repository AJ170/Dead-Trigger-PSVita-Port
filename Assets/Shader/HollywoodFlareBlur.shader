Shader "Hidden/HollywoodFlareBlurShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_NonBlurredTex ("Base (RGB)", 2D) = "" {}
	}
	SubShader { 
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			Fog { Mode Off }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 tintColor;
            sampler2D _MainTex;
            sampler2D _NonBlurredTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float2 tmpvar_1;
                float2 tmpvar_2;
                tmpvar_2 = v.uv.xy;
                tmpvar_1 = tmpvar_2;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                float4 colorNb_2;
                float4 color_3;
                float4 tmpvar_4;
                tmpvar_4 = tex2D (_MainTex, i.uv);
                color_3 = tmpvar_4;
                float4 tmpvar_5;
                tmpvar_5 = tex2D (_NonBlurredTex, i.uv);
                colorNb_2 = tmpvar_5;
                float4 tmpvar_6;
                tmpvar_6 = normalize(tintColor);
                tmpvar_1 = (((color_3 * tintColor) * 0.5) + ((colorNb_2 * tmpvar_6) * 0.5));
                return tmpvar_1;
            }
            ENDCG
        }
    }
}