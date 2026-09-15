Shader "MADFINGER/FX/ScreenDropsSheen" {
	Properties {
		_Color ("Drop Tint", Color) = (0.62, 0.72, 0.82, 0.30)
		_SpecColor2 ("Spec Color", Color) = (1, 1, 1, 1)
		_SpecPower ("Spec Power", Float) = 24
		_SpecStrength ("Spec Strength", Float) = 0.9
		_RimStrength ("Rim Strength", Float) = 0.55
		_LightDir ("Light Dir (xy)", Vector) = (-0.45, 0.6, 0.66, 0)
	}
	// Cheap path: a plain alpha-blended overlay mesh. No RenderTexture, no full-screen
	// blit, no grab pass - the droplet normal is generated procedurally from the quad's
	// local coords, so a wet-glass sheen costs only the drop's own pixels.
	SubShader {
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			Lighting Off
			Fog { Color (0,0,0,0) }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			fixed4 _SpecColor2;
			float _SpecPower;
			float _SpecStrength;
			float _RimStrength;
			float4 _LightDir;

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
				// Mesh is authored in screen space; emit clip space directly.
				o.pos = float4(v.vertex.x, -(v.vertex.y), 0.0, 1.0);
				o.local = v.uv.xy;
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

				// Procedural dome normal: treat the drop as a hemisphere bulging out of
				// the screen. This is the "normal map" without paying for a texture fetch.
				float z = sqrt(saturate(1.0 - saturate(r * r)));
				float3 n = normalize(float3(p.x, p.y, z + 0.35));

				float3 L = normalize(_LightDir.xyz);
				float3 V = float3(0.0, 0.0, 1.0);          // viewer looks straight at screen
				float3 H = normalize(L + V);

				// Specular glint - the highlight that sells "wet".
				float spec = pow(saturate(dot(n, H)), _SpecPower) * _SpecStrength;
				// Fresnel-ish rim so the drop edge catches light.
				float rim = pow(1.0 - saturate(dot(n, V)), 3.0) * _RimStrength;

				float fade = saturate(i.intensity);
				float alpha = saturate(_Color.a + spec + rim) * mask * fade;
				fixed3 col = _Color.rgb + _SpecColor2.rgb * spec + _SpecColor2.rgb * rim * 0.5;

				return fixed4(col, alpha);
			}
			ENDCG
		}
	}
	Fallback Off
}
