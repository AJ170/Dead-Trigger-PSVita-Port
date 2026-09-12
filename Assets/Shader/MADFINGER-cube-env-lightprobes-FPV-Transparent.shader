Shader "MADFINGER/Environment/Cube env map (Supports LightProbes) FPV - Transparent"
{
	Properties
	{
		_MainTex("Base (RGB) Alpha (A)", 2D) = "white" {}
		_EnvTex("Cube Env Tex", CUBE) = "black" {}
		_SHLightingScale("LightProbe Influence Scale", Float) = 1.0
		_EnvStrength("Env Strength", Float) = 1.0
		_UVScrollSpeed("UV Scroll Speed XY", Vector) = (0, 0, 0, 0)

		// SH coefficients injected by LightProbeSamplerDT
		[HideInInspector] _SHAr("SH Ar", Vector) = (0, 0, 0, 0)
		[HideInInspector] _SHAg("SH Ag", Vector) = (0, 0, 0, 0)
		[HideInInspector] _SHAb("SH Ab", Vector) = (0, 0, 0, 0)
		[HideInInspector] _SHBr("SH Br", Vector) = (0, 0, 0, 0)
		[HideInInspector] _SHBg("SH Bg", Vector) = (0, 0, 0, 0)
		[HideInInspector] _SHBb("SH Bb", Vector) = (0, 0, 0, 0)
		[HideInInspector] _SHC("SH C", Vector) = (0, 0, 0, 0)
	}

	SubShader
	{
		LOD 100
		Tags { "LightMode" = "ForwardBase" "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"

			sampler2D   _MainTex;
			samplerCUBE _EnvTex;

			float4 _MainTex_ST;
			float4 _UVScrollSpeed;
			half   _SHLightingScale;
			half   _EnvStrength;

			// Custom SH coefficients from LightProbeSamplerDT
			float4 _SHAr;
			float4 _SHAg;
			float4 _SHAb;
			float4 _SHBr;
			float4 _SHBg;
			float4 _SHBb;
			float4 _SHC;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
				float3 normal : NORMAL;
				half4  color  : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos      : SV_POSITION;
				float2 uv       : TEXCOORD0;
				half3  cubeUV   : TEXCOORD1; // Reflection vector
				half3  shColor  : TEXCOORD2; // Baked SH result
				half   gloss    : TEXCOORD3; // Alpha channel for reflection
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// ===== SPHERICAL HARMONICS EVALUATION =====
			// Evaluate SH using custom property block coefficients
			// xzy swizzle converts Unity world space to GL/SH convention
			half3 EvaluateCustomSH(half3 worldNormal)
			{
				half3 n = worldNormal.xzy;
				float4 nv = float4(n, 1.0);

				// L0 + L1 linear terms
				half3 x1;
				x1.r = dot(_SHAr, nv);
				x1.g = dot(_SHAg, nv);
				x1.b = dot(_SHAb, nv);

				// L2 quadratic terms
				float4 vB = nv.xyzz * nv.yzzx;
				half3 x2;
				x2.r = dot(_SHBr, vB);
				x2.g = dot(_SHBg, vB);
				x2.b = dot(_SHBb, vB);

				// Final L2 quadratic
				float vC = n.x * n.x - n.y * n.y;
				half3 x3 = _SHC.rgb * vC;

				return x1 + x2 + x3;
			}

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);

				// UV with scrolling
				o.uv = TRANSFORM_TEX(v.uv, _MainTex)
					 + frac(_UVScrollSpeed.xy * _Time.xy);

				// World space normal and position
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				// Reflection vector for cubemap
				// Negate X to match Unity left-handed convention
				float3 viewDir = worldPos - _WorldSpaceCameraPos;
				float3 refl = reflect(viewDir,
					float3(worldNormal.x, worldNormal.y, worldNormal.z));
				o.cubeUV = half3(-refl.x, refl.y, refl.z);

				// SH lighting with scale and vertex colour tint
				half3 sh = EvaluateCustomSH(worldNormal);
				o.shColor = sh * _SHLightingScale * v.color.rgb;

				// Pass gloss (alpha) through for reflection blend in frag
				o.gloss = v.color.a;

				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				half4 mainTex = tex2D(_MainTex, i.uv);

				// Sample cubemap reflection
				half3 cubeColor = texCUBE(_EnvTex, i.cubeUV).rgb;

				// Blend reflection by texture alpha (gloss mask) and env strength
				half reflectAmount = mainTex.a * _EnvStrength;

				// Final colour — diffuse modulated by SH,
				// plus cubemap reflection weighted by gloss
				half3 finalColor = (mainTex.rgb * i.shColor)
									+ (cubeColor * reflectAmount);

				// ===== TRANSPARENCY =====
				// Use texture alpha multiplied by the global alpha slider
				//half finalAlpha = mainTex.a;// * _Alpha * i.gloss;

				return half4(finalColor, mainTex.a);
			}
			ENDCG
		}
	}

	Fallback "Transparent/VertexLit"
}
