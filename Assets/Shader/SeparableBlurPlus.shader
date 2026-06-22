Shader "Hidden/SeparableBlurPlus" {
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
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                float4 uv4 : TEXCOORD4;
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
                o.uv1 = (v.uv.xyxy + (offsets.xyxy * float4(1.0, 1.0, -1.0, -1.0)));
                o.uv2 = (v.uv.xyxy + (float4(2.0, 2.0, -2.0, -2.0) * offsets.xyxy));
                o.uv3 = (v.uv.xyxy + (float4(3.0, 3.0, -3.0, -3.0) * offsets.xyxy));
                o.uv4 = (v.uv.xyxy + (float4(7.0, 7.0, -7.0, -7.0) * offsets.xyxy));

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = tex2D (_MainTex, i.uv);
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv1.xy);
                float4 tmpvar_3;
                tmpvar_3 = tex2D (_MainTex, i.uv1.zw);
                float4 tmpvar_4;
                tmpvar_4 = tex2D (_MainTex, i.uv2.xy);
                float4 tmpvar_5;
                tmpvar_5 = tex2D (_MainTex, i.uv2.zw);
                float4 tmpvar_6;
                tmpvar_6 = tex2D (_MainTex, i.uv3.xy);
                float4 tmpvar_7;
                tmpvar_7 = tex2D (_MainTex, i.uv3.zw);
                float4 tmpvar_8;
                tmpvar_8 = tex2D (_MainTex, i.uv4.xy);
                float4 tmpvar_9;
                tmpvar_9 = tex2D (_MainTex, i.uv4.zw);
                return (((((((((0.25 * tmpvar_1) + (0.15 * tmpvar_2)) + (0.15 * tmpvar_3)) + (0.11 * tmpvar_4)) + (0.11 * tmpvar_5)) + (0.075 * tmpvar_6)) + (0.075 * tmpvar_7)) + (0.04 * tmpvar_8)) + (0.04 * tmpvar_9));
            }
            ENDCG
        }
    }
}