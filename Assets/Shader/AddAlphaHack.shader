Shader "Hidden/AddAlphaHack" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader { 
		Tags { "QUEUE"="Overlay+1000" "RenderType"="Opaque" }
		Pass {
			Tags { "QUEUE"="Overlay+1000" "RenderType"="Opaque" }
			ZWrite Off
			Blend One One

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                tmpvar_1 = v.uv.xy;
                float4 tmpvar_2;
                tmpvar_2.zw = float2(0.0, 0.0);
                tmpvar_2.x = tmpvar_1.x;
                tmpvar_2.y = tmpvar_1.y;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = mul(UNITY_MATRIX_TEXTURE0, tmpvar_2).xy;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                tmpvar_1 = (float4(0.0, 0.0, 0.0, 1.0) * tmpvar_2);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}