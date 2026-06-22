Shader "MADFINGER/Environment/Skybox - opaque - no fog" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader { 
		LOD 100
		Tags { "QUEUE"="Geometry+10" "RenderType"="Opaque" }
		Pass {
			Tags { "QUEUE"="Geometry+10" "RenderType"="Opaque" }
			ZWrite Off
			Fog { Mode Off }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _MainTex_ST;

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

                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = tex2D (_MainTex, i.uv);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}