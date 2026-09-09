Shader "MADFINGER/Self-Illumin/Diffuse LightProbe"
{
	Properties
	{
		_Color("Main Color", Color) = (1, 1, 1, 1)
		_ColorMult("Color Multiplier", Float) = 1.0
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_EmissionLM("Emission (Lightmapper)", Float) = 1.5
		//_CubeTex("Specular Cubemap", CUBE) = "black" {}
	}

	SubShader
	{
		LOD 100
		Tags { "RenderType" = "Opaque" }

		Pass
		{
			Tags { "RenderType" = "Opaque" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			// ===== PROPERTIES =====
			float4 _Color;
			float _ColorMult;
			half _EmissionLM;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform float4 _ScreenTint;  // Post-process color tint

			// ===== SPHERICAL HARMONICS COEFFICIENTS (from LightProbeSamplerDT) =====
			// These are pushed by LightProbeSamplerDT via MaterialPropertyBlock
			uniform float4 _SHAr;  // SH A coefficients - Red channel
			uniform float4 _SHAg;  // SH A coefficients - Green channel
			uniform float4 _SHAb;  // SH A coefficients - Blue channel
			uniform float4 _SHBr;  // SH B coefficients - Red channel
			uniform float4 _SHBg;  // SH B coefficients - Green channel
			uniform float4 _SHBb;  // SH B coefficients - Blue channel
			uniform float4 _SHC;   // SH C coefficients

			// =========== CUBE MAP FOR EXTRA BLING!=================
            //samplerCUBE _CubeTex;

			// ===== VERTEX INPUT =====
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// ===== VERTEX TO FRAGMENT =====
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 colorMultiplied : TEXCOORD1;  // Pre-multiplied color
				float3 worldNormal : TEXCOORD2;     // For SH lighting
				UNITY_FOG_COORDS(3)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// ===== SPHERICAL HARMONICS EVALUATION =====
			// Evaluates SH lighting using the coefficients provided by LightProbeSamplerDT
			float3 EvaluateSphericalHarmonics(float3 normal)
			{
				// Evaluate second-order spherical harmonics
				float4 vNormal = float4(normal, 1.0);

				// SH A (first order)
				float3 shA = float3(
					dot(_SHAr, vNormal),
					dot(_SHAg, vNormal),
					dot(_SHAb, vNormal)
				);

				// SH B (second order)
				float3 shB = float3(
					dot(_SHBr, vNormal),
					dot(_SHBg, vNormal),
					dot(_SHBb, vNormal)
				);

				// SH C (second order zonal)
				float3 shC = _SHC.rgb * (normal.x * normal.x + normal.y * normal.y - 2.0 * normal.z * normal.z);

				// Combine all contributions
				return max(shA + shB + shC, float3(0, 0, 0));
			}

			// ===== VERTEX SHADER =====
			v2f vert(appdata v)
			{
				v2f output;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// Transform vertex to clip space
				output.pos = UnityObjectToClipPos(v.vertex);

				// Pass through UVs
				output.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// Pre-calculate color multiplier
				output.colorMultiplied = _Color * _ColorMult;

				// Calculate world space normal (for SH evaluation)
				output.worldNormal = UnityObjectToWorldNormal(v.normal);

				// Transfer fog coordinates
				UNITY_TRANSFER_FOG(output, output.pos);

				return output;
			}

			// ===== FRAGMENT SHADER =====
			half4 frag(v2f input) : SV_Target
			{
				// Sample base texture
				half4 texColor = tex2D(_MainTex, input.uv);

				// Apply color multiplier
				half4 baseColor = texColor * input.colorMultiplied;

				// ===== LIGHT PROBE SAMPLING (LightProbeSamplerDT Integration) =====
				// Evaluate spherical harmonics using world normal
				float3 lightProbeLight = EvaluateSphericalHarmonics(input.worldNormal);

				// Apply light probe to base color
				baseColor.rgb *= lightProbeLight;

				// ===== EMISSION & POST-PROCESS TINT =====
				// Apply emission/lightmapper multiplier
				baseColor.rgb *= _EmissionLM;

				// Apply screen tint (post-process color correction)
				baseColor.rgb += _ScreenTint.rgb;

				// ===== FOG =====
				UNITY_APPLY_FOG(input.fogCoord, baseColor);

				return baseColor;
			}
			ENDCG
		}
	}

	Fallback "Mobile/Diffuse"
}

