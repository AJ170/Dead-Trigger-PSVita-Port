Shader "Hidden/AddStuff" {
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

            sampler2D _MainTex;
            float intensity;

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
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                tmpvar_1 = (tmpvar_2 * intensity);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}