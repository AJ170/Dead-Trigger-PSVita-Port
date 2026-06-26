Shader "Vita/FasterMobileBloomWithDirt"
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

	uniform sampler2D _DirtTex;
	uniform half _DirtIntensity;

	uniform half2 _ThresholdParams;
	uniform half  _Spread;

	uniform sampler2D _BloomTex;
	uniform half _BloomIntensity;

	struct v2fCombineBloom
	{
		float4 pos : SV_POSITION;
		half2  uv  : TEXCOORD0;
#if UNITY_UV_STARTS_AT_TOP
		half2  uv2 : TEXCOORD1;
#endif
	};

	struct v2fBlurDown
	{
		float4 pos  : SV_POSITION;
		half2  uv0  : TEXCOORD0;
		half4  uv12 : TEXCOORD1;
		half4  uv34 : TEXCOORD2;
	};

	struct v2fBlurUp
	{
		float4 pos  : SV_POSITION;
		half4  uv12 : TEXCOORD0;
		half4  uv34 : TEXCOORD1;
		half4  uv56 : TEXCOORD2;
		half4  uv78 : TEXCOORD3;
	};

	v2fBlurDown vertBlurDown(appdata_img v)
	{
		v2fBlurDown o;
		o.pos = UnityObjectToClipPos(v.vertex);

		half2 baseUV = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
		half2 offset = _MainTex_TexelSize.xy * _Spread;

		o.uv0 = baseUV;
		o.uv12.xy = baseUV + half2(offset.x, offset.y);
		o.uv12.zw = baseUV + half2(-offset.x, offset.y);
		o.uv34.xy = baseUV + half2(-offset.x, -offset.y);
		o.uv34.zw = baseUV + half2(offset.x, -offset.y);

		return o;
	}

	v2fBlurUp vertBlurUp(appdata_img v)
	{
		v2fBlurUp o;
		o.pos = UnityObjectToClipPos(v.vertex);

		half2 baseUV = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
		half2 offset = _MainTex_TexelSize.xy * _Spread;
		half2 offset2 = offset * 2.0h;

		o.uv12.xy = baseUV + half2(offset.x, offset.y);
		o.uv12.zw = baseUV + half2(-offset.x, offset.y);
		o.uv34.xy = baseUV + half2(-offset.x, -offset.y);
		o.uv34.zw = baseUV + half2(offset.x, -offset.y);
		o.uv56.xy = baseUV + half2(0.0h, offset2.y);
		o.uv56.zw = baseUV + half2(0.0h, -offset2.y);
		o.uv78.xy = baseUV + half2(offset2.x, 0.0h);
		o.uv78.zw = baseUV + half2(-offset2.x, 0.0h);

		return o;
	}

	v2fCombineBloom vertCombineBloom(appdata_img v)
	{
		v2fCombineBloom o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
#if UNITY_UV_STARTS_AT_TOP
		o.uv2 = o.uv;
		if (_MainTex_TexelSize.y < 0.0)
			o.uv.y = 1.0 - o.uv.y;
#endif

		return o;
	}

	// Optimized: Reduced multiplications, reordered operations for better instruction pairing
	half4 fragBlurDownFirstPass(v2fBlurDown i) : SV_Target
	{
		half4 col0 = tex2D(_MainTex, i.uv0);
		half4 col1 = tex2D(_MainTex, i.uv12.xy);
		half4 col2 = tex2D(_MainTex, i.uv12.zw);
		half4 col3 = tex2D(_MainTex, i.uv34.xy);
		half4 col4 = tex2D(_MainTex, i.uv34.zw);

		// Combine multiplications: 0.5 * 0.25 = 0.125
		half4 col = col0 * 0.5h + (col1 + col2 + col3 + col4) * 0.125h;
		col += _ThresholdParams.y;
		col = max(col, 0.0h);

		return col;
	}

		// Optimized: Combined multiplications
		half4 fragBlurDown(v2fBlurDown i) : SV_Target
	{
		half4 col0 = tex2D(_MainTex, i.uv0);
		half4 col1 = tex2D(_MainTex, i.uv12.xy);
		half4 col2 = tex2D(_MainTex, i.uv12.zw);
		half4 col3 = tex2D(_MainTex, i.uv34.xy);
		half4 col4 = tex2D(_MainTex, i.uv34.zw);

		// Combine multiplications: 0.5 * 0.25 = 0.125, 0.5 * 1.0 = 0.5
		half4 col = col0 * 0.5h + (col1 + col2 + col3 + col4) * 0.125h;

		return col;
	}

		// Optimized: Reduced to fewer samples (tent filter instead of 8-tap)
		// If you need maximum speed, use this simplified version
		half4 fragBlurUpFast(v2fBlurDown i) : SV_Target
	{
		half4 col0 = tex2D(_MainTex, i.uv0);
		half4 col1 = tex2D(_MainTex, i.uv12.xy);
		half4 col2 = tex2D(_MainTex, i.uv12.zw);
		half4 col3 = tex2D(_MainTex, i.uv34.xy);
		half4 col4 = tex2D(_MainTex, i.uv34.zw);

		// Simple box blur
		return (col0 + col1 + col2 + col3 + col4) * 0.2h;
	}

		// Original quality version with optimized math
		half4 fragBlurUp(v2fBlurUp i) : SV_Target
	{
		half4 col1 = tex2D(_MainTex, i.uv12.xy);
		half4 col2 = tex2D(_MainTex, i.uv12.zw);
		half4 col3 = tex2D(_MainTex, i.uv34.xy);
		half4 col4 = tex2D(_MainTex, i.uv34.zw);
		half4 col5 = tex2D(_MainTex, i.uv56.xy);
		half4 col6 = tex2D(_MainTex, i.uv56.zw);
		half4 col7 = tex2D(_MainTex, i.uv78.xy);
		half4 col8 = tex2D(_MainTex, i.uv78.zw);

		// Optimized: Group operations for better ALU utilization
		half4 col = (col1 + col2 + col3 + col4) * 0.3333333h + (col5 + col6 + col7 + col8) * 0.1666666h;

		return col;
	}

		// Optimized: Simplified blending
// Modified combine bloom with dirt layer
half4 fragCombineBloom(v2fCombineBloom i) : SV_Target
	{
	#if UNITY_UV_STARTS_AT_TOP
		half4 col = tex2D(_MainTex, i.uv2);
		half4 bloom = tex2D(_BloomTex, i.uv);
	#else
		half4 col = tex2D(_MainTex, i.uv);
		half4 bloom = tex2D(_BloomTex, i.uv);
	#endif

		// Sample dirt texture in screen space and apply as convolution
		half4 dirt = tex2D(_DirtTex, i.uv);
		bloom *= lerp(1.0h, dirt, _DirtIntensity);

		return col + bloom * _BloomIntensity;
	}

		ENDCG
		SubShader
	{
		Cull Off ZWrite Off ZTest Always

			//initial downscale and threshold
			Pass
		{
CGPROGRAM
#pragma vertex vertBlurDown
#pragma fragment fragBlurDownFirstPass
ENDCG
		}

			//down pass
			Pass
		{
CGPROGRAM
#pragma vertex vertBlurDown
#pragma fragment fragBlurDown
ENDCG
		}

			//up pass (use fragBlurUpFast for better performance)
			Pass
		{
CGPROGRAM
#pragma vertex vertBlurUp
#pragma fragment fragBlurUp
// For maximum performance, change to:
// #pragma vertex vertBlurDown
// #pragma fragment fragBlurUpFast
ENDCG
		}

			//final bloom
			Pass
		{
CGPROGRAM
#pragma vertex vertCombineBloom
#pragma fragment fragCombineBloom
ENDCG
		}
	}
}