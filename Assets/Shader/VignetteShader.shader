Shader "Hidden/VignetteShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
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

            sampler2D _MainTex;
            float vignetteIntensity;

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
                float4 color_2;
                float2 uv_3;
                float2 coords_4;
                coords_4 = i.uv;
                uv_3 = i.uv;
                float2 tmpvar_5;
                tmpvar_5 = ((coords_4 - 0.5) * 2.0);
                coords_4 = tmpvar_5;
                float tmpvar_6;
                tmpvar_6 = dot (tmpvar_5, tmpvar_5);
                float4 tmpvar_7;
                tmpvar_7 = tex2D (_MainTex, uv_3);
                color_2 = tmpvar_7;
                float tmpvar_8;
                tmpvar_8 = (1.0 - (tmpvar_6 * vignetteIntensity));
                tmpvar_1 = (color_2 * tmpvar_8);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}