Shader "MADFINGER/Environment/Scroll 2 Layers Additive (Supports Lightmap)" {
	Properties {
		_MainTex ("Base layer (RGB)", 2D) = "white" {}
		_DetailTex ("2nd layer (RGB)", 2D) = "white" {}
		_ScrollX ("Base layer Scroll speed X", Float) = 1
		_ScrollY ("Base layer Scroll speed Y", Float) = 0
		_Scroll2X ("2nd layer Scroll speed X", Float) = 1
		_Scroll2Y ("2nd layer Scroll speed Y", Float) = 0
		_AMultiplier ("Layer Multiplier", Float) = 0.5
	}
	SubShader { 
		LOD 100
		Tags { "RenderType"="Opaque" }
		Pass {
			Tags { "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _ScrollX;
            float _ScrollY;
            float _Scroll2X;
            float _Scroll2Y;
            float _AMultiplier;

            sampler2D _MainTex;
            sampler2D _DetailTex;

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
                float4 uv2 : TEXCOORD2;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float2 tmpvar_2;
                tmpvar_2.x = _ScrollX;
                tmpvar_2.y = _ScrollY;
                float2 tmpvar_3;
                tmpvar_3.x = _Scroll2X;
                tmpvar_3.y = _Scroll2Y;
                float4 tmpvar_4;
                tmpvar_4.x = _AMultiplier;
                tmpvar_4.y = _AMultiplier;
                tmpvar_4.z = _AMultiplier;
                tmpvar_4.w = _AMultiplier;
                tmpvar_1 = tmpvar_4;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = (v.uv.xy + frac((tmpvar_2 * _Time.xy)));
                o.uv1 = (v.uv.xy + frac((tmpvar_3 * _Time.xy)));
                o.uv2 = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = ((tex2D (_MainTex, i.uv) + tex2D (_DetailTex, i.uv1)) * i.uv2);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}