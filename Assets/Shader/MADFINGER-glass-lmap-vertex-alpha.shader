Shader "MADFINGER/Glass/Lightmap + Cube Env + Per Vertex Alpha (Optimized)" {
	Properties {
		_MainTex ("Diffuse (RGB) Alpha (A)", 2D) = "white" {}
		_EnvMap ("Environment Map", CUBE) = "black" {}
		_ReflectivityMaskWeights ("Reflectivity Mask Weights (RGB)", Vector) = (0.3, 0.59, 0.11, 0)
		_FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.5
	}
	SubShader { 
		LOD 100
		Tags { "QUEUE" = "Transparent" "IGNOREPROJECTOR" = "true" "RenderType" = "Transparent" }
		Pass {
			Tags { "QUEUE" = "Transparent" "IGNOREPROJECTOR" = "true" "RenderType" = "Transparent" }
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			samplerCUBE _EnvMap;
			half4 _ReflectivityMaskWeights;
			half _FresnelStrength;

			uniform vector _ScreenTint;

			struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float4 color : COLOR;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				half3 worldNormal : TEXCOORD2;
				half3 worldViewDir : TEXCOORD3;
				float4 color : COLOR;
			};

			v2f vert(appdata_t v) {
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);

				// UV coordinates
				o.uv = v.uv.xy;
				o.uv1 = (v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw;

				// World space normal
				o.worldNormal = UnityObjectToWorldNormal(v.normal);

				// World space view direction
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldViewDir = _WorldSpaceCameraPos - worldPos;

				// Vertex alpha
				o.color = v.color;

				return o;
			}

			half4 frag(v2f i) : SV_Target {
				// Sample base texture
				half4 baseColor = tex2D(_MainTex, i.uv);

				// Normalize interpolated world-space values
				half3 worldNormal = normalize(i.worldNormal);
				half3 worldViewDir = normalize(i.worldViewDir);

				// Calculate reflection vector
				half3 reflectionVector = reflect(-worldViewDir, worldNormal);

				// Sample cubemap environment
				half3 envColor = texCUBE(_EnvMap, reflectionVector).rgb;

				// === REFLECTIVITY MASK ===
				// Calculate luminance of base color as reflectivity mask
				half reflectivityMask = dot(baseColor.rgb, _ReflectivityMaskWeights.rgb);

				// === FRESNEL EFFECT ===
				// Fresnel: reflections are stronger at grazing angles
				// Using Schlick's approximation: F = F0 + (1 - F0) * (1 - NdotV)^2
				half NdotV = saturate(dot(worldNormal, worldViewDir));
				half oneMinusNdotV = 1.0 - NdotV;
				half fresnel = _FresnelStrength + (1.0 - _FresnelStrength) * (oneMinusNdotV * oneMinusNdotV);

				// Apply reflectivity mask and fresnel to environment contribution
				half3 reflectedColor = envColor * reflectivityMask * fresnel;

				// Combine base color with reflected environment
				half3 finalColor = baseColor.rgb + reflectedColor;

				// Apply lightmap
				half3 lightmap = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1));
				finalColor *= (2.0 * lightmap);

				// Output with per-vertex alpha
				return half4(finalColor, baseColor.a * i.color.a) + half4(_ScreenTint.xyz, 0.0);
			}
			ENDCG
		}
	}
}
