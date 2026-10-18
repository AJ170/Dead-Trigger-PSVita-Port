Shader "MADFINGER/PostFX/WaterScreenRefraction" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_BumpMap ("Bump Map", 2D) = "" {}
		_BumpStrength("Normal Strength", Range(0, 2)) = 1.0
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
			sampler2D _BumpMap;
			float4 _BumpMap_ST;
			float _BumpStrength;

			struct appdata_t
			{
				float4 vertex : POSITION;   // xy in [-1,1] screen space; z = intensity (0..1)
				float2 uv : TEXCOORD0;      // -1..1 local drop coords (0 = drop centre)
				float2 tileOrigin : TEXCOORD2; // uv3: tile origin in the normal map (0..1)
				float2 tileSize : TEXCOORD3;   // uv4: tile size in the normal map (0..1)
				fixed4 color  : COLOR;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;       // screen sample coords (with refraction offset)
				float2 local : TEXCOORD1;    // local drop coords, for the round mask
				float intensity : TEXCOORD2;
				float2 bumpUV : TEXCOORD3;  // Normal map UV, mapped into this drop's tile
				fixed4 color : COLOR;
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				// Lens normal from the local coords -> refraction direction.
				float3 norm = -normalize(float3(v.uv.xy, 0.25));

				o.pos = float4(v.vertex.x, -(v.vertex.y), 0.0, 1.0);

				o.uv = ComputeScreenPos(o.pos);

				o.local = v.uv.xy  * 0.5 + 0.5;
				o.intensity = v.vertex.z;

				float2 tileSize =  v.tileSize;// (v.tileSize.x + v.tileSize.y) > 0.0 ? v.tileSize : float2(1.0, 1.0);
				float2 tileOrigin =  v.tileOrigin; //(v.tileSize.x + v.tileSize.y) > 0.0 ? v.tileOrigin : float2(0.0, 0.0);
				o.bumpUV = tileOrigin + (v.uv.xy * 0.5 + 0.5) * tileSize;

				o.color = v.color;

				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				//float2 normalUV = i.local * 0.5 + 0.5;
				float4 normalSample = tex2D(_BumpMap, i.bumpUV);

				// Unpack normal and apply strength
				float3 n = UnpackNormal(normalSample);
				n.xy *= _BumpStrength;
				n = normalize(n);

				float3 col = tex2D(_MainTex, i.uv + n.xy * i.color.r).rgb + (_Color.rgb * i.intensity);
				//return normalSample;
				return half4(col, normalSample.a);
			}
			ENDCG
		}
	}
}
