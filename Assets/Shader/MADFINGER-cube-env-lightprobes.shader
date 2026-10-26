Shader "Vita/Environment/Cube Env Map - LightProbe SH" {
 	Properties
	{
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_EnvTex("Cube Env Tex", CUBE) = "black" {}
		_SHLightingScale("LightProbe Influence Scale", Float) = 1.0
		_EnvStrength("Env Strength", Float) = 1.0
		_UVScrollSpeed("UV Scroll Speed XY", Vector) = (0,0,0,0)
	}

	SubShader
	{
		LOD 100
		Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }

		Pass
		{
			Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			sampler2D   _MainTex;
			samplerCUBE _EnvTex;

			float4 _MainTex_ST;
			float4 _UVScrollSpeed;
			half   _SHLightingScale;
			half   _EnvStrength;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
				float3 normal : NORMAL;
				half4  color  : COLOR;
			};

			struct v2f
			{
				float4 pos      : SV_POSITION;
				float2 uv       : TEXCOORD0;
				half3  cubeUV   : TEXCOORD1; // Reflection vector
				half3  shColor  : TEXCOORD2; // Light probe result
				half   gloss    : TEXCOORD3; // Alpha channel for reflection
			};

			v2f vert(appdata_t v)
			{
				v2f o;

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
				float3 refl = normalize(reflect(viewDir, worldNormal));
				o.cubeUV = half3(-refl.x, refl.y, refl.z);

				// Unity's built-in light probe SH for this object, evaluated per vertex
				// (the same path the standard mobile shaders use for ambient)
				half3 sh = ShadeSH9(half4(worldNormal, 1.0));
				o.shColor = sh * _SHLightingScale * v.color.rgb;

				// Pass gloss (alpha) through for reflection blend in frag
				o.gloss = v.color.a;

				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				half4 mainTex = tex2D(_MainTex, i.uv);

				// Sample cubemap reflection
				half3 cubeColor = texCUBE(_EnvTex, i.cubeUV).rgb;

				// Blend reflection by texture alpha (gloss mask) and env strength
				half reflectAmount = mainTex.a * _EnvStrength;

				// Diffuse modulated by probe SH, plus cubemap reflection weighted by gloss
				half3 finalColor = (mainTex.rgb * i.shColor)
				                 + (cubeColor * reflectAmount);

				return half4(finalColor, 1.0);
			}
			ENDCG
		}
	}

	Fallback "Mobile/Diffuse"
}
