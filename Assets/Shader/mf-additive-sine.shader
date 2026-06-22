Shader "MADFINGER/FX/Additive Sine" {
	Properties {
		_MainTex ("Texture", 2D) = "black" {}
		_ScrollSpeed ("Scroll speed XY", Vector) = (0,0,0,0)
		_SineParams ("Sine params: amplitude XY, freq ZW", Vector) = (1,1,0.5,0.5)
	}
	SubShader { 
		Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Fog {
				Color (0,0,0,0)
			}
			Blend One One

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _SineParams;
            float4 _ScrollSpeed;
            float4 _MainTex_ST;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
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

                float2 tmpvar_1;
                float2 tmpvar_2;
                tmpvar_2 = (((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw) + frac((_ScrollSpeed.xy * _Time.y)));
                tmpvar_1.x = (tmpvar_2.x + (sin((_Time.y * _SineParams.z)) * _SineParams.x));
                tmpvar_1.y = (tmpvar_2.y + (sin((_Time.y * _SineParams.w)) * _SineParams.y));
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.color = v.color;
                o.uv = tmpvar_1;

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