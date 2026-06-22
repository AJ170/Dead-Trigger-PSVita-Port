Shader "__CAPA__/Laser" {
	Properties {
		_MainTex ("MainTex", 2D) = "white" {}
		_NoiseTex ("NoiseTex", 2D) = "white" {}
	}
	SubShader { 
		Tags { "QUEUE"="Transparent" "RenderType"="Transparent" "Reflection"="LaserScope" }
		Pass {
			Tags { "QUEUE"="Transparent" "RenderType"="Transparent" "Reflection"="LaserScope" }
			ZWrite Off
			Cull Off
			Blend SrcAlpha One

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _MainTex_ST;
            float4 _NoiseTex_ST;

            sampler2D _MainTex;
            sampler2D _NoiseTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float2 tmpvar_2;
                tmpvar_2 = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                tmpvar_1.xy = tmpvar_2;
                float2 tmpvar_3;
                tmpvar_3 = ((v.uv.xy * _NoiseTex_ST.xy) + _NoiseTex_ST.zw);
                tmpvar_1.zw = tmpvar_3;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = (tex2D (_MainTex, i.uv.xy) * tex2D (_NoiseTex, i.uv.zw));
                return tmpvar_1;
            }
            ENDCG
        }
    }
}