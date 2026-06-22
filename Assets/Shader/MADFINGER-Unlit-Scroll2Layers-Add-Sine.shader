Shader "MADFINGER/Environment/Scroll 2 Layers Additive No Lightmap Sine" {
	Properties {
		_MainTex ("Base layer (RGB)", 2D) = "white" {}
		_DetailTex ("2nd layer (RGB)", 2D) = "white" {}
		_ScrollX ("Base layer Scroll speed X", Float) = 1
		_ScrollY ("Base layer Scroll speed Y", Float) = 0
		_Scroll2X ("2nd layer Scroll speed X", Float) = 1
		_Scroll2Y ("2nd layer Scroll speed Y", Float) = 0
		_SineAmplX ("Base layer sine amplitude X", Float) = 0.5
		_SineAmplY ("Base layer sine amplitude Y", Float) = 0.5
		_SineFreqX ("Base layer sine freq X", Float) = 10
		_SineFreqY ("Base layer sine freq Y", Float) = 10
		_SineAmplX2 ("2nd layer sine amplitude X", Float) = 0.5
		_SineAmplY2 ("2nd layer sine amplitude Y", Float) = 0.5
		_SineFreqX2 ("2nd layer sine freq X", Float) = 10
		_SineFreqY2 ("2nd layer sine freq Y", Float) = 10
		_MMultiplier ("Layer Multiplier", Float) = 2
	}
	SubShader { 
		LOD 100
		Tags { "RenderType"="Opaque" }
		Pass {
			Tags { "RenderType"="Opaque" }
			Fog { Mode Off }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _MainTex_ST;
            float4 _DetailTex_ST;
            float _ScrollX;
            float _ScrollY;
            float _Scroll2X;
            float _Scroll2Y;
            float _MMultiplier;
            float _SineAmplX;
            float _SineAmplY;
            float _SineFreqX;
            float _SineFreqY;
            float _SineAmplX2;
            float _SineAmplY2;
            float _SineFreqX2;
            float _SineFreqY2;

            sampler2D _MainTex;
            sampler2D _DetailTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float4 tmpvar_2;
                float2 tmpvar_3;
                tmpvar_3.x = _ScrollX;
                tmpvar_3.y = _ScrollY;
                tmpvar_1.xy = (((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw) + frac((tmpvar_3 * _Time.xy)));
                float2 tmpvar_4;
                tmpvar_4.x = _Scroll2X;
                tmpvar_4.y = _Scroll2Y;
                tmpvar_1.zw = (((v.uv.xy * _DetailTex_ST.xy) + _DetailTex_ST.zw) + frac((tmpvar_4 * _Time.xy)));
                tmpvar_1.x = (tmpvar_1.x + (sin((_Time * _SineFreqX)) * _SineAmplX).x);
                tmpvar_1.y = (tmpvar_1.y + (sin((_Time * _SineFreqY)) * _SineAmplY).x);
                tmpvar_1.z = (tmpvar_1.z + (sin((_Time * _SineFreqX2)) * _SineAmplX2).x);
                tmpvar_1.w = (tmpvar_1.w + (sin((_Time * _SineFreqY2)) * _SineAmplY2).x);
                float4 tmpvar_5;
                tmpvar_5 = (_MMultiplier * v.color);
                tmpvar_2 = tmpvar_5;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;
                o.uv1 = tmpvar_2;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = ((tex2D (_MainTex, i.uv.xy) + tex2D (_DetailTex, i.uv.zw)) * i.uv1);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}