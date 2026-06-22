Shader "MADFINGER/FX/Blood FX - alpha blended" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_FadeInSpeed ("Fade in speed", Float) = 5
		_DrippingSpeed ("Dripping speed", Float) = 0.1
		_UsrTime ("Time", Float) = 0
	}
	SubShader { 
		Tags { "QUEUE"="Overlay" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Overlay" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Cull Off
			Fog {
				Color (0,0,0,0)
			}
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _DrippingSpeed;
            float _UsrTime;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float4 tmpvar_2;
                float tmpvar_3;
                tmpvar_3 = (_UsrTime + v.vertex.z);
                float4 tmpvar_4;
                tmpvar_4.zw = float2(0.0, 1.0);
                tmpvar_4.xy = v.vertex.xy;
                tmpvar_1.xzw = tmpvar_4.xzw;
                float4 tmpvar_5;
                tmpvar_5.xyz = float3(1.0, 1.0, 1.0);
                tmpvar_5.w = (1.0 - max ((tmpvar_3 - (0.25 * v.uv1.x)), 0.0));
                tmpvar_2 = tmpvar_5;
                tmpvar_1.y = (v.vertex.y - ((tmpvar_3 * _DrippingSpeed) * v.uv1.y));
                o.pos = tmpvar_1;
                o.uv = v.uv.xy;
                o.color = tmpvar_2;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = (tex2D (_MainTex, i.uv) * i.color);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}