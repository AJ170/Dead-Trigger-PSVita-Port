Shader "Hidden/LensFlareCreate" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	SubShader { 
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			Fog { Mode Off }
			Blend One One

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float4 color0;
            float4 colorA;
            float4 colorB;
            float4 colorC;
            float4 colorD;
            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float2 uv4 : TEXCOORD4;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float2 tmpvar_1;
                float2 tmpvar_2;
                float2 tmpvar_3;
                float2 tmpvar_4;
                float2 tmpvar_5;
                float2 tmpvar_6;
                tmpvar_6 = v.uv.xy;
                tmpvar_1 = tmpvar_6;
                float2 tmpvar_7;
                tmpvar_7 = (((v.uv.xy - 0.5) * -0.85) + 0.5);
                tmpvar_2 = tmpvar_7;
                float2 tmpvar_8;
                tmpvar_8 = (((v.uv.xy - 0.5) * -1.45) + 0.5);
                tmpvar_3 = tmpvar_8;
                float2 tmpvar_9;
                tmpvar_9 = (((v.uv.xy - 0.5) * -2.55) + 0.5);
                tmpvar_4 = tmpvar_9;
                float2 tmpvar_10;
                tmpvar_10 = (((v.uv.xy - 0.5) * -4.15) + 0.5);
                tmpvar_5 = tmpvar_10;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;
                o.uv1 = tmpvar_2;
                o.uv2 = tmpvar_3;
                o.uv3 = tmpvar_4;
                o.uv4 = tmpvar_5;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 color_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                float4 tmpvar_3;
                tmpvar_3 = (tmpvar_2 * color0);
                color_1 = tmpvar_3;
                float4 tmpvar_4;
                tmpvar_4 = tex2D (_MainTex, i.uv1);
                float4 tmpvar_5;
                tmpvar_5 = (color_1 + (tmpvar_4 * colorA));
                color_1 = tmpvar_5;
                float4 tmpvar_6;
                tmpvar_6 = tex2D (_MainTex, i.uv2);
                float4 tmpvar_7;
                tmpvar_7 = (color_1 + (tmpvar_6 * colorB));
                color_1 = tmpvar_7;
                float4 tmpvar_8;
                tmpvar_8 = tex2D (_MainTex, i.uv3);
                float4 tmpvar_9;
                tmpvar_9 = (color_1 + (tmpvar_8 * colorC));
                color_1 = tmpvar_9;
                float4 tmpvar_10;
                tmpvar_10 = tex2D (_MainTex, i.uv4);
                float4 tmpvar_11;
                tmpvar_11 = (color_1 + (tmpvar_10 * colorD));
                color_1 = tmpvar_11;
                return color_1;
            }
            ENDCG
        }
    }
}