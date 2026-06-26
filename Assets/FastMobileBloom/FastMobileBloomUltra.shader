Shader "Vita/FastMobileBloomUltra"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BloomTex("Bloom (RGB)", 2D) = "black" {}
	}
		CGINCLUDE
#include "UnityCG.cginc"
		uniform sampler2D _MainTex;
	uniform half4 _MainTex_TexelSize;
	uniform	half4 _MainTex_ST;

	uniform half2 _ThresholdParams;
	uniform half  _Spread;

	uniform sampler2D _BloomTex;
	uniform half _BloomIntensity;

	// Ultra-simple vertex output - minimal interpolators
	struct v2fSimple
	{
		float4 pos : SV_POSITION;
		half2  uv  : TEXCOORD0;
	};

	struct v2fBlur
	{
		float4 pos : SV_POSITION;
		half4  uv01 : TEXCOORD0;  // Only 2 offset pairs instead of 4
		half4  uv23 : TEXCOORD1;
	};

	// Minimal vertex shader
	v2fSimple vertSimple(appdata_img v)
	{
		v2fSimple o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
		return o;
	}

	// Optimized 4-tap blur vertex shader
	v2fBlur vertBlur(appdata_img v)
	{
		v2fBlur o;
		o.pos = UnityObjectToClipPos(v.vertex);

		half2 baseUV = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
		half2 offset = _MainTex_TexelSize.xy * _Spread;

		// Only 4 taps in cross pattern (cheaper than 5-tap tent)
		o.uv01.xy = baseUV + half2(0.0h, offset.y);   // top
		o.uv01.zw = baseUV + half2(0.0h, -offset.y);   // bottom
		o.uv23.xy = baseUV + half2(offset.x, 0.0h);   // right
		o.uv23.zw = baseUV + half2(-offset.x, 0.0h);   // left

		return o;
	}

	// Ultra-fast threshold + downsample (single texture sample with threshold)
	half4 fragThreshold(v2fSimple i) : SV_Target
	{
		half4 col = tex2D(_MainTex, i.uv);
		col += _ThresholdParams.y;
		return max(col, 0.0h);
	}

		// Ultra-fast 4-tap blur (instead of 5 or 8)
		half4 fragBlur(v2fBlur i) : SV_Target
	{
		half4 col0 = tex2D(_MainTex, i.uv01.xy);
		half4 col1 = tex2D(_MainTex, i.uv01.zw);
		half4 col2 = tex2D(_MainTex, i.uv23.xy);
		half4 col3 = tex2D(_MainTex, i.uv23.zw);

		// Simple average of 4 samples
		return (col0 + col1 + col2 + col3) * 0.25h;
	}

		// Minimal additive blend
		half4 fragCombine(v2fSimple i) : SV_Target
	{
		half4 col = tex2D(_MainTex, i.uv);
		half4 bloom = tex2D(_BloomTex, i.uv);
		return col + bloom * _BloomIntensity;
	}

		ENDCG
		SubShader
	{
		Cull Off ZWrite Off ZTest Always

			// Pass 0: Threshold only (no blur)
			Pass
		{
CGPROGRAM
#pragma vertex vertSimple
#pragma fragment fragThreshold
ENDCG
		}

			// Pass 1: 4-tap blur
			Pass
		{
CGPROGRAM
#pragma vertex vertBlur
#pragma fragment fragBlur
ENDCG
		}

			// Pass 2: Same 4-tap blur (reuse for upscale)
			Pass
		{
CGPROGRAM
#pragma vertex vertBlur
#pragma fragment fragBlur
ENDCG
		}

			// Pass 3: Final combine
			Pass
		{
CGPROGRAM
#pragma vertex vertSimple
#pragma fragment fragCombine
ENDCG
		}
	}
}