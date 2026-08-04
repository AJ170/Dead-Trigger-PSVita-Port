Shader "MADFINGER/Environment/Virtual Gloss Per-Vertex Additive (Supports Lightmap, Optimized)" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_SpecOffset ("Specular Offset from Camera", Vector) = (1, 10, 2, 0)
		_SpecRange ("Specular Range", Float) = 20
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range(0.01, 1)) = 0.078125
		_ScrollingSpeed ("Scrolling Speed", Vector) = (0, 0, 0, 0)
	}

	SubShader {
		Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
		LOD 100

		CGINCLUDE
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		float3 _SpecOffset;
		float _SpecRange;
		float3 _SpecColor;
		float _Shininess;
		float4 _ScrollingSpeed;

		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			#ifndef LIGHTMAP_OFF
			float2 uvLightmap : TEXCOORD1;
			#endif
			half3 specular : TEXCOORD2;
			UNITY_FOG_COORDS(3)
		};

		v2f vert(appdata_full v) {
			v2f o;

			o.pos = UnityObjectToClipPos(v.vertex);

			// Scrolling UVs
			o.uv = v.texcoord.xy + frac(_ScrollingSpeed.xy * _Time.y);

			// === PER-VERTEX SPECULAR IN CAMERA SPACE ===
			// Transform normal and position to camera/view space
			half3 viewNormal = mul((half3x3)UNITY_MATRIX_MV, v.normal);
			float4 viewPos = mul(UNITY_MATRIX_MV, v.vertex);

			// Virtual camera-space light position
			// (1, 10, 2) offset, negate Z for camera-forward convention
			float3 virtualLightPos = _SpecOffset * float3(1.0, 1.0, -1.0);

			// Vector from light to surface in view space
			float3 dirToLight = viewPos.xyz - virtualLightPos;

			// Blinn-Phong half-vector
			// Camera is always looking forward (0, 0, 1) in view space
			float3 viewDir = float3(0.0, 0.0, 1.0);
			float3 halfVector = normalize(viewDir + normalize(-dirToLight));

			// Specular attenuation based on distance from virtual light
			float distance = length(dirToLight);
			float attenuation = 1.0 - saturate(distance / _SpecRange);

			// Calculate Blinn-Phong specular (pre-compute shininess exponent)
			float shininessExponent = _Shininess * 128.0;
			half NdotH = saturate(dot(viewNormal, halfVector));
			half specularBrightness = pow(NdotH, shininessExponent);

			// Combine specular: color * brightness * attenuation * 2.0
			o.specular = half3(_SpecColor) * specularBrightness * attenuation * 2.0;

			// Lightmap UVs
			#ifndef LIGHTMAP_OFF
			o.uvLightmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#endif

			// Transfer fog coordinates
			UNITY_TRANSFER_FOG(o, o.pos);

			return o;
		}
		ENDCG

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma fragmentoption ARB_precision_hint_fastest

			fixed4 frag(v2f i) : SV_Target {
				// Sample base texture
				fixed4 baseColor = tex2D(_MainTex, i.uv);

				// Extract specular from alpha channel and multiply by specular brightness
				fixed3 specular = i.specular.rgb * baseColor.a;

				// Add specular to base color
				baseColor.rgb += specular;

				// Apply lightmap if available
				#ifndef LIGHTMAP_OFF
				fixed3 lightmap = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uvLightmap));
				baseColor.rgb *= lightmap;
				#endif

				// Apply fog
				UNITY_APPLY_FOG(i.fogCoord, baseColor.rgb);

				return baseColor;
			}
			ENDCG
		}
	}
}
