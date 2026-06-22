Shader "MADFINGER/Diffuse/Vertex Colored" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader { 
		LOD 100
		Tags { "RenderType"="Opaque" }
		Pass {
			Tags { "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;

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

                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = v.uv.xy;
                o.color = ((v.color * _Color) * 2.0);

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