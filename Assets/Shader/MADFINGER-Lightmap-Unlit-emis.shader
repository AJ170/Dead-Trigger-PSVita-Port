Shader "MADFINGER/Environment/Lightmap + emissivity" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Emissivity ("Emissivity scaler", Range(0,4)) = 1
	}
	SubShader { 
		LOD 100
		Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }
		Pass {
			Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float _Emissivity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = v.uv.xy;
                o.uv1 = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.w = tmpvar_2.w;
                float3 tmpvar_3;
                tmpvar_3 = (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz);
                float3 tmpvar_4;
                tmpvar_4 = (tmpvar_2.xyz * (tmpvar_3 + (tmpvar_2.www * _Emissivity)));
                c_1.xyz = tmpvar_4;
                return c_1;
            }
            ENDCG
        }
    }
}