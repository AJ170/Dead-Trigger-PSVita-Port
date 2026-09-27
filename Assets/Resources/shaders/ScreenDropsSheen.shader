Shader "MADFINGER/FX/ScreenDropsSheen" {
	Properties {
		_Color ("Drop Tint", Color) = (0.62, 0.72, 0.82, 0.30)
		_SpecColor2 ("Spec Color", Color) = (1, 1, 1, 1)
		// Default "bump" is a flat normal, so with nothing assigned the shader falls
		// back to the procedural dome below and looks exactly as it did before.
		_BumpMap ("Droplet Normal", 2D) = "bump" {}
		_BumpStrength ("Normal Strength", Range(0, 2)) = 1
		_Cube ("Environment Cube", Cube) = "" {}
		_CubeStrength ("Cube Strength", Range(0, 3)) = 0.6
		// How much environment shows at the drop's centre (1 = flat across the whole
		// drop, 0 = rim only). Keep it above zero or the cube all but disappears.
		_CubeCenter ("Cube Centre Amount", Range(0, 1)) = 0.35
		// Fraction of the mask the hemisphere spans. Below 1 so the steep grazing
		// angles stay inside the visible area.
		_DomeRadius ("Dome Radius", Range(0.3, 1)) = 0.85
		_SpecPower ("Spec Power", Float) = 24
		_SpecStrength ("Spec Strength", Float) = 0.6
		_RimStrength ("Rim Strength", Float) = 0.55
		_FresnelPower ("Fresnel Power", Float) = 1.5
		_LightDir ("Light Dir (xy)", Vector) = (-0.45, 0.6, 0.66, 0)
		// +1 or -1. This shader emits clip space directly instead of going through the
		// projection matrix, so the clip-space Y convention cannot be inferred - it has
		// to be told. ScreenDrops sets it per platform. See the vertex shader.
		_FlipY ("Clip Y Sign", Float) = 1
	}
	// Cheap path: a plain alpha-blended overlay mesh. No RenderTexture, no full-screen
	// blit, no grab pass - which is the whole point, since the Vita dislikes pulling the
	// framebuffer back out of the pipeline mid-render.
	//
	// Instead of sampling what is actually behind the drop, the drop's normal drives a
	// lookup into a static environment cubemap. That keeps the "this drop is a little
	// lens catching the world" read that sells wet glass, at the cost of one cube fetch
	// on the drop's own pixels only. The Ultra path (MADFINGER/PostFX/WaterScreenRefraction)
	// is untouched and still does true screen refraction.
	SubShader {
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			Lighting Off
			Fog { Color (0,0,0,0) }
			// Premultiplied alpha. This is what lets the cubemap specular be genuinely
			// ADDITIVE inside a single transparent pass: the drop body is written as
			// rgb*alpha and composited over dst*(1-alpha), while the highlight is added
			// straight on top without being attenuated by the drop's own alpha.
			Blend One OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			fixed4 _SpecColor2;
			sampler2D _BumpMap;
			float _BumpStrength;
			samplerCUBE _Cube;
			float _CubeStrength;
			float _CubeCenter;
			float _DomeRadius;
			float _SpecPower;
			float _SpecStrength;
			float _RimStrength;
			float _FresnelPower;
			float4 _LightDir;
			float _FlipY;

			struct appdata_t {
				float4 vertex : POSITION;   // xy = screen NDC, z = intensity (0..1)
				float2 uv : TEXCOORD0;      // -1..1 local drop coords
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 local : TEXCOORD0;
				float intensity : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				// Mesh is authored in screen space; emit clip space directly. _FlipY
				// carries the platform's clip-space Y convention - with it inverted the
				// whole quad mirrors vertically, which makes drops appear to rise.
				o.pos = float4(v.vertex.x, -(v.vertex.y) * _FlipY, 0.0, 1.0);
				// The local coords must mirror with the quad, or the teardrop's taper
				// would end up pointing the wrong way on the flipped platform.
				o.local = float2(v.uv.x, v.uv.y * _FlipY);
				o.intensity = v.vertex.z;
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{
				// Teardrop mask (rounded bottom, soft taper to the top), same shape the
				// refraction path uses so both quality levels look consistent.
				float taper = 1.0 - 0.35 * saturate(i.local.y);
				float2 p = float2(i.local.x / max(taper, 0.05), i.local.y);
				float r = length(p);
				float mask = 1.0 - smoothstep(0.6, 1.0, r);
				if (mask <= 0.0)
					discard;

				// Base shape: treat the drop as a hemisphere bulging out of the screen.
				// The dome radius is scaled to _DomeRadius (< 1) so the hemisphere's
				// grazing angles land INSIDE the visible part of the mask. With the dome
				// spanning the full 0..1 the steep rim sat out at r > 0.9, where the mask
				// has already faded to nothing - which left every edge-weighted term
				// (fresnel, rim, cube) multiplied by ~0.001 and therefore invisible.
				float rd = saturate(r / max(_DomeRadius, 0.05));
				float z = sqrt(saturate(1.0 - rd * rd));
				float3 n = normalize(float3(p.x, p.y, z + 0.1));

				// Perturb with the authored normal map. Local coords are -1..1, so remap
				// to 0..1 for the fetch. Whiteout blend keeps the dome's silhouette while
				// letting the map add surface detail (beading, trails, flattening).
				float2 bumpUV = i.local * 0.5 + 0.5;
				float3 nm = UnpackNormal(tex2D(_BumpMap, bumpUV));
				nm.xy *= _BumpStrength;
				n = normalize(float3(n.xy + nm.xy, n.z * nm.z));

				float3 L = normalize(_LightDir.xyz);
				float3 V = float3(0.0, 0.0, 1.0);          // viewer looks straight at screen
				float3 H = normalize(L + V);

				// Direct specular glint - the highlight that sells "wet".
				float spec = pow(saturate(dot(n, H)), _SpecPower) * _SpecStrength;
				// Fresnel-ish rim so the drop edge catches light.
				float fresnel = pow(1.0 - saturate(dot(n, V)), _FresnelPower);
				float rim = fresnel * _RimStrength;

				// Environment reflection. Rotating the reflection vector into world space
				// keeps the cubemap anchored to the level rather than smeared across the
				// drop as the player turns, which is what makes it read as a reflection.
				float3 refl = reflect(-V, n);
				refl = mul((float3x3)unity_CameraToWorld, refl);
				// A drop is a lens, not a mirror bead: the environment shows across the
				// whole drop and merely intensifies toward the rim. Weighting it by raw
				// fresnel would confine it to an edge the mask has already faded out.
				float envWeight = lerp(_CubeCenter, 1.0, fresnel);
				fixed3 env = texCUBE(_Cube, refl).rgb * _CubeStrength * envWeight;

				float fade = saturate(i.intensity);
				float a = saturate(_Color.a + spec + rim) * mask * fade;

				// Premultiplied: body scaled by alpha, highlights added on top.
				fixed3 body = _Color.rgb * a;
				fixed3 add = (_SpecColor2.rgb * (spec + rim * 0.5) + env) * mask * fade;

				return fixed4(body + add, a);
			}
			ENDCG
		}
	}
	Fallback Off
}
