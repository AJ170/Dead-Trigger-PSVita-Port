Shader "MADFINGER/Self-Illumin/Diffuse" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_ColorMult ("Color mult", Float) = 1
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_EmissionLM ("Emission (Lightmapper)", Float) = 0
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
            float _ColorMult;

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
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float4 tmpvar_2;
                tmpvar_2 = (_Color * _ColorMult);
                tmpvar_1 = tmpvar_2;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = v.uv.xy;
                o.uv1 = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = (tex2D (_MainTex, i.uv) * i.uv1);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}