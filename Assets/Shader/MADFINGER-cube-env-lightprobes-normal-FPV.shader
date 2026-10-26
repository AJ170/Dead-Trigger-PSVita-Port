Shader "Vita/Character/Cube env map Normal (Supports LightProbes) FPV"
{
	Properties
	{
		_MainTex("Diffuse (RGB)", 2D) = "grey" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Scale", Range(0, 2)) = 1.0

		_CubeTex("Specular Cubemap", CUBE) = "black" {}
		_SpecularStrength("Specular Strength", Range(0, 2)) = 0.6
		_SpecularBase("Specular Base", Range(0, 1)) = 0.05
		_SpecularSharpness("Specular Sharpness", Range(1, 8)) = 3.0

		// Ambient floor so probe-dark areas never go fully black
		_Ambient("Ambient Floor", Color) = (0.05, 0.05, 0.05, 1)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
		LOD 200

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			sampler2D   _MainTex;
			sampler2D   _BumpMap;
			samplerCUBE _CubeTex;

			float4 _MainTex_ST;
			float4 _BumpMap_ST;
			half   _BumpScale;
			half   _SpecularStrength;
			half   _SpecularBase;
			half   _SpecularSharpness;
			half4  _Ambient;

			uniform float4 _ScreenTint;

			struct appdata
			{
				float4 vertex  : POSITION;
				float3 normal  : NORMAL;
				float4 tangent : TANGENT;
				float2 uv      : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos          : SV_POSITION;
				float4 uv           : TEXCOORD0; // xy = main, zw = bump
				half3  tSpace0      : TEXCOORD1; // TBN row 0
				half3  tSpace1      : TEXCOORD2; // TBN row 1
				half3  tSpace2      : TEXCOORD3; // TBN row 2
				half3  worldViewDir : TEXCOORD4;
				float3 worldPos     : TEXCOORD5;
				UNITY_FOG_COORDS(6)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Per-pixel ambient from light probes. Uses the probe volume at this pixel's
			// world position when the object is inside one, otherwise the per-object SH.
			// UNITY_LIGHT_PROBE_PROXY_VOLUME is set by Unity when the probe volume is in use;
			// confirm the macro name against UnityShaderVariables.cginc in your Unity 2017 install.
			half3 ShadeProbeAmbient(half3 worldNormal, float3 worldPos)
			{
			/*
			#if defined(UNITY_LIGHT_PROBE_PROXY_VOLUME)
				if (unity_ProbeVolumeParams.x == 1.0)
				{
					return SHEvalLinearL0L1_SampleProbeVolume(half4(worldNormal, 1.0), worldPos);
				}
			#endif*/
				return ShadeSH9(half4(worldNormal, 1.0));
			}

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);

				// Build TBN for per-pixel normal reconstruction
				half3 worldNormal  = UnityObjectToWorldNormal(v.normal);
				half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);

				worldTangent = normalize(worldTangent - worldNormal * dot(worldNormal, worldTangent));

				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

				o.tSpace0 = half3(worldTangent.x, worldBinormal.x, worldNormal.x);
				o.tSpace1 = half3(worldTangent.y, worldBinormal.y, worldNormal.y);
				o.tSpace2 = half3(worldTangent.z, worldBinormal.z, worldNormal.z);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldPos = worldPos;
				o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);

				UNITY_TRANSFER_FOG(o, o.pos);

				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				half4 diffuse = tex2D(_MainTex, i.uv.xy);

				// Unpack and scale the bump normal, then move it to world space
				half3 tangentNormal = UnpackNormal(tex2D(_BumpMap, i.uv.zw));
				tangentNormal.xy *= _BumpScale;
				tangentNormal = normalize(tangentNormal);

				half3 worldNormal;
				worldNormal.x = dot(i.tSpace0, tangentNormal);
				worldNormal.y = dot(i.tSpace1, tangentNormal);
				worldNormal.z = dot(i.tSpace2, tangentNormal);
				worldNormal = normalize(worldNormal);

				half3 worldViewDir = normalize(i.worldViewDir);

				// === LIGHT PROBES (per pixel, bump-normal based) ===
				half3 probeLight = ShadeProbeAmbient(worldNormal, i.worldPos);
				half3 lighting = max(probeLight, _Ambient.rgb);

				// === CUBEMAP SPECULAR ===
				half3 reflVec = reflect(-worldViewDir, worldNormal);
				reflVec = normalize(reflVec);
				reflVec.x = -reflVec.x;

				half3 cubeColor = texCUBE(_CubeTex, reflVec).rgb;

				// Variable sharpness via lerp between NdotR and NdotR^4
				half NdotR = saturate(dot(worldNormal, normalize(reflVec)));
				half spec2 = NdotR * NdotR;
				half spec4 = spec2 * spec2;
				half spec = lerp(NdotR, spec4, _SpecularSharpness / 8.0);

				half3 specular = saturate(_SpecularBase + cubeColor * spec * _SpecularStrength * diffuse.a);

				// === FINAL ===
				half3 finalColor = diffuse.rgb * lighting + specular;

				UNITY_APPLY_FOG(i.fogCoord, finalColor);

				return half4(finalColor, 1.0) + _ScreenTint;
			}
			ENDCG
		}
	}

	Fallback "Mobile/Diffuse"
}
