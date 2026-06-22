Shader "Hidden/BrightPassFilterShader" {
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
            float4 threshhold;
            float useSrcAlphaAsMask;

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
                float4 tmpvar_3;
                tmpvar_3 = tex2D (_MainTex, i.uv);
                color_2 = tmpvar_3;
                float4 tmpvar_4;
                tmpvar_4 = (clamp ((color_2 - threshhold.x), 0.0, 1.0) * threshhold.y);
                color_2 = tmpvar_4;
                float4 tmpvar_5;
                tmpvar_5 = clamp ((color_2 * lerp (1.0, color_2.w, useSrcAlphaAsMask)), 0.0, 1.0);
                tmpvar_1 = tmpvar_5;
                return tmpvar_1;
            }
            ENDCG
        }
    }
}