Shader "MADFINGER/FX/ScreenDropsSheen Normal-Map Driven"
{
	Properties
	{
		_Color("Drop Tint", Color) = (0.62, 0.72, 0.82, 0.30)
		_SpecColor2("Spec Color", Color) = (1, 1, 1, 1)

		_BumpMap("Droplet Normal (RGB) Alpha (A)", 2D) = "bump" {}
		_BumpStrength("Normal Strength", Range(0, 2)) = 1.0

		_Cube("Environment Cube", Cube) = "" {}
		_CubeStrength("Cube Strength", Range(0, 3)) = 0.6
		_CubeCenter("Cube Centre Amount", Range(0, 1)) = 0.35

		_SpecPower("Spec Power", Float) = 24
		_SpecStrength("Spec Strength", Float) = 0.6
		_RimStrength("Rim Strength", Float) = 0.55
		_FresnelPower("Fresnel Power", Float) = 1.5

		_LightDir("Light Dir (xyz)", Vector) = (-0.45, 0.6, 0.66, 0)

		// +1 or -1. Clip-space Y convention (platform-dependent)
		_FlipY("Clip Y Sign", Float) = 1
	}

	SubShader
	{
		Tags { "Queue" = "Overlay" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Pass
		{
			ZTest Always
			ZWrite Off
			Cull Off
			Lighting Off
			Fog { Color(0, 0, 0, 0) }
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			fixed4 _SpecColor2;
			sampler2D _BumpMap;
			float4 _BumpMap_ST;
			float _BumpStrength;
			samplerCUBE _Cube;
			float _CubeStrength;
			float _CubeCenter;
			float _SpecPower;
			float _SpecStrength;
			float _RimStrength;
			float _FresnelPower;
			float4 _LightDir;
			float _FlipY;

			struct appdata_t
			{
				float4 vertex : POSITION;   // xy = screen NDC, z = intensity (0..1)
				float2 uv : TEXCOORD0;      // -1..1 local drop coords
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 local : TEXCOORD0;   // Local drop coordinates for normal map UV
				float3 viewDir : TEXCOORD1; // View direction (always toward camera)
				float intensity : TEXCOORD2;
			};

			v2f vert(appdata_t v)
			{
				v2f o;

				// Emit clip space directly
				o.pos = float4(v.vertex.x, -(v.vertex.y) * _FlipY, 0.0, 1.0);

				// Local drop coordinates (-1..1) — used as normal map UV
				o.local = v.uv.xy;

				// View direction — always pointing toward camera in screen space
				// Droplets are oriented to face the view so reflections change with viewing angle
				//o.viewDir = -UNITY_MATRIX_V[2].xyz; //Get the forward view direction of the camera matrix to use for the speculars

				o.viewDir = WorldSpaceViewDir(v.vertex);//normalize(WorldSpaceViewDir(v.vertex) + o.viewDir);

				o.intensity = v.vertex.z;

				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				// ===== NORMAL MAP SAMPLING =====
				// The normal map fully drives the surface appearance
				// Local coords are -1..1, remap to 0..1 for texture fetch
				float2 normalUV = i.local * 0.5 + 0.5;
				float4 normalSample = tex2D(_BumpMap, normalUV);

				// Unpack normal and apply strength
				float3 n = UnpackNormal(normalSample);
				n.xy *= _BumpStrength;
				n = normalize(n);

				// ===== ALPHA FROM NORMAL MAP =====
				// Extract alpha channel for transparency variation
				// This allows the normal map to control where the droplet is transparent
				float mapAlpha = normalSample.a;

				// ===== ENVIRONMENT REFLECTION =====
				// Reflect view direction across the surface normal
				// This is where the normal map fully controls the cubemap lookup
				float3 refl = reflect(-i.viewDir, n);

				// Rotate reflection into world space so cubemap stays anchored to level
				// This makes the reflection change as the player looks around
				refl = mul((float3x3)unity_CameraToWorld, refl);

				fixed4 specCube = texCUBE(_Cube, refl);

				// ===== ALPHA COMPOSITION =====
				// Combine drop base alpha with specular/rim highlights
				float fade = saturate(i.intensity);
				float a = saturate(_Color.a * fade * mapAlpha);

				// ===== FINAL COLOR =====
				// Premultiplied: body scaled by alpha, highlights added on top
				fixed3 body = _Color.rgb * a;

				return fixed4(specCube * normalSample.a * fade);

				return fixed4(body, a);
				//fixed3 add = (_SpecColor2.rgb) * fade * mapAlpha;

				//return fixed4(body + add, a);
			}
			ENDCG
		}
	}

	Fallback Off
}
