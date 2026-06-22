Shader "Hidden/HollywoodFlareStretchShader" {
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

            float4 offsets;
            float stretchWidth;
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
                float2 P_5;
                P_5 = (i.uv + ((stretchWidth * 2.0) * offsets.xy));
                tmpvar_4 = tex2D (_MainTex, P_5);
                float4 tmpvar_6;
                float2 P_7;
                P_7 = (i.uv - ((stretchWidth * 2.0) * offsets.xy));
                tmpvar_6 = tex2D (_MainTex, P_7);
                float4 tmpvar_8;
                float2 P_9;
                P_9 = (i.uv + ((stretchWidth * 4.0) * offsets.xy));
                tmpvar_8 = tex2D (_MainTex, P_9);
                float4 tmpvar_10;
                float2 P_11;
                P_11 = (i.uv - ((stretchWidth * 4.0) * offsets.xy));
                tmpvar_10 = tex2D (_MainTex, P_11);
                float4 tmpvar_12;
                float2 P_13;
                P_13 = (i.uv + ((stretchWidth * 8.0) * offsets.xy));
                tmpvar_12 = tex2D (_MainTex, P_13);
                float4 tmpvar_14;
                float2 P_15;
                P_15 = (i.uv - ((stretchWidth * 8.0) * offsets.xy));
                tmpvar_14 = tex2D (_MainTex, P_15);
                float4 tmpvar_16;
                float2 P_17;
                P_17 = (i.uv + ((stretchWidth * 14.0) * offsets.xy));
                tmpvar_16 = tex2D (_MainTex, P_17);
                float4 tmpvar_18;
                float2 P_19;
                P_19 = (i.uv - ((stretchWidth * 14.0) * offsets.xy));
                tmpvar_18 = tex2D (_MainTex, P_19);
                float4 tmpvar_20;
                float2 P_21;
                P_21 = (i.uv + ((stretchWidth * 20.0) * offsets.xy));
                tmpvar_20 = tex2D (_MainTex, P_21);
                float4 tmpvar_22;
                float2 P_23;
                P_23 = (i.uv - ((stretchWidth * 20.0) * offsets.xy));
                tmpvar_22 = tex2D (_MainTex, P_23);
                float4 tmpvar_24;
                tmpvar_24 = max (max (max (max (max (max (max (max (max (max (color_2, tmpvar_4), tmpvar_6), tmpvar_8), tmpvar_10), tmpvar_12), tmpvar_14), tmpvar_16), tmpvar_18), tmpvar_20), tmpvar_22);
                color_2 = tmpvar_24;
                tmpvar_1 = tmpvar_24;
                return tmpvar_1;
            }
            ENDCG
        }
    }
}