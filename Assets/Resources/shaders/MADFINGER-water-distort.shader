Shader "MADFINGER/PostFX/WaterScreenRefraction" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_EnvMap ("2D EnvMap", 2D) = "black" {}
		_ScrollingSpeed ("xy - Layer0, zw - Layer1", Vector) = (0,0.05,0,0.01)
		_Color ("Color", Color) = (0,0,0,0)
		_Params ("x = refraction strength, y = Layer 0 tiling, z = Layer 1 tiling", Vector) = (0.01,1.5,2,0)
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

            float4 _Params;
            float4 _Color;

            sampler2D _MainTex;
            sampler2D _EnvMap;

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
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 norm_1;
                float2 tmpvar_2;
                float4 tmpvar_3;
                float3 tmpvar_4;
                tmpvar_4.z = 0.25;
                tmpvar_4.xy = v.uv.xy;
                float3 tmpvar_5;
                tmpvar_5 = -(normalize(tmpvar_4));
                norm_1 = tmpvar_5;
                float4 tmpvar_6;
                tmpvar_6.zw = float2(0.0, 1.0);
                tmpvar_6.x = v.vertex.x;
                tmpvar_6.y = -(v.vertex.y);
                float2 tmpvar_7;
                tmpvar_7 = ((((v.vertex.xy * 0.5) + 0.5) + (0.5 / _ScreenParams.xy)) + (norm_1.xy * _Params.x));
                tmpvar_2.x = tmpvar_7.x;
                tmpvar_2.y = (1.0 - tmpvar_7.y);
                float4 tmpvar_8;
                tmpvar_8 = ((v.vertex.z * 2.0) * _Color);
                tmpvar_3 = tmpvar_8;
                o.pos = tmpvar_6;
                o.uv = tmpvar_2;
                o.uv1 = norm_1.xy;
                o.color = tmpvar_3;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = ((tex2D (_MainTex, i.uv) + tex2D (_EnvMap, i.uv1)) + i.color);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}