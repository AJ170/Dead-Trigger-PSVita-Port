Shader "MADFINGER/Particles/Additive TwoSide" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
}

// Rewritten from a fixed-function (BindChannels/SetTexture) shader to a
// programmable CG shader. The fixed-function combiner path does not render
// on PS Vita (GXM), which made every effect that relied solely on this
// shader invisible (bullet impacts, sparks, smoke, etc.). Modeled on the
// project's working "MADFINGER/Particles/Additive TwoSided FPV" shader.
SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Pass {
		Blend SrcAlpha One
		AlphaTest Greater .01
		ColorMask RGB
		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Color (0,0,0,0) }

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		fixed4 _TintColor;

		struct appdata_t {
			float4 vertex : POSITION;
			fixed4 color : COLOR;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			fixed4 color : COLOR;
			float2 texcoord : TEXCOORD0;
		};

		v2f vert (appdata_t v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			// constant(_TintColor) * primary(vertex color), then * texture DOUBLE
			o.color = v.color * _TintColor * 2.0;
			o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
			return o;
		}

		fixed4 frag (v2f i) : SV_Target
		{
			return tex2D(_MainTex, i.texcoord) * i.color;
		}
		ENDCG
	}
}
Fallback Off
}
