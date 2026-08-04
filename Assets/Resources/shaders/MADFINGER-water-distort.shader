Shader "MADFINGER/PostFX/WaterScreenRefraction" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_EnvMap ("2D EnvMap", 2D) = "black" {}
		_ScrollingSpeed ("xy - Layer0, zw - Layer1", Vector) = (0,0.05,0,0.01)
		_Color ("Color", Color) = (0,0,0,0)
		_Params ("x = refraction strength, y = Layer 0 tiling, z = Layer 1 tiling", Vector) = (0.01,1.5,2,0)
	}
	SubShader {
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			Fog { Mode Off }
			// Round, soft-edged drops that blend with the scene (was opaque -> hard squares).
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 _Params;
			float4 _Color;
			sampler2D _MainTex;

			struct appdata_t
			{
				float4 vertex : POSITION;   // xy in [-1,1] screen space; z = intensity (0..1)
				float2 uv : TEXCOORD0;      // -1..1 local drop coords (0 = drop centre)
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;       // screen sample coords (with refraction offset)
				float2 local : TEXCOORD1;    // local drop coords, for the round mask
				float intensity : TEXCOORD2;
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				// Lens normal from the local coords -> refraction direction.
				float3 norm = -normalize(float3(v.uv.xy, 0.25));
				o.pos = float4(v.vertex.x, -(v.vertex.y), 0.0, 1.0);
				// Screen position of this vertex, plus the refraction offset (scaled by
				// intensity so fading drops distort less).
				float2 scr = ((v.vertex.xy * 0.5) + 0.5) + (0.5 / _ScreenParams.xy);
				scr += norm.xy * (_Params.x * v.vertex.z);
				o.uv = float2(scr.x, 1.0 - scr.y);
				o.local = v.uv.xy;
				o.intensity = v.vertex.z;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				// Teardrop mask: rounded at the bottom, tapering to a point at the top
				// (the tail trails upward as the drop slides down the screen). local.y = +1
				// is the top of the drop on screen, so we pinch the horizontal width there.
				// Lower pinch factor = blunter, softer tip (0.85 was a needle point).
				float taper = 1.0 - 0.35 * saturate(i.local.y);
				float r = length(float2(i.local.x / max(taper, 0.05), i.local.y));
				float a = (1.0 - smoothstep(0.6, 1.0, r)) * saturate(i.intensity);
				float3 col = tex2D(_MainTex, i.uv).rgb + (_Color.rgb * i.intensity);
				return half4(col, a);
			}
			ENDCG
		}
	}
}
