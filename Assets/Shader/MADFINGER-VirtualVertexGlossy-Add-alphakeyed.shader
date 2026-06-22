// Upgrade NOTE: replaced 'glstate_matrix_modelview0' with 'UNITY_MATRIX_MV'
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Environment/Virtual Gloss Per-Vertex Additive AlphaKeyed (Supports Lightmap)" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_SpecOffset ("Specular Offset from Camera", Vector) = (1,10,2,0)
		_SpecRange ("Specular Range", Float) = 20
		_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Shininess ("Shininess", Range(0.01,1)) = 0.078125
		_SpecStrength ("Specular Strength", Float) = 2
	}
	SubShader { 
		LOD 100
		Tags { "LIGHTMODE"="ForwardBase" "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "LIGHTMODE"="ForwardBase" "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float4 _MainTex_ST;
            float3 _SpecOffset;
            float _SpecRange;
            float3 _SpecColor;
            float _Shininess;
            float _SpecStrength;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 uv2 : TEXCOORD2;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float3x3 tmpvar_2;
                tmpvar_2[0] = UNITY_MATRIX_MV[0].xyz;
                tmpvar_2[1] = UNITY_MATRIX_MV[1].xyz;
                tmpvar_2[2] = UNITY_MATRIX_MV[2].xyz;
                float3 tmpvar_3;
                tmpvar_3 = (mul(UNITY_MATRIX_MV, v.vertex).xyz - (_SpecOffset * float3(1.0, 1.0, -1.0)));
                float4 tmpvar_4;
                tmpvar_4 = (((_SpecColor * pow (clamp (dot (mul(tmpvar_2, normalize(v.normal)), normalize(((float3(0.0, 0.0, 1.0) + normalize(-(tmpvar_3))) * 0.5))), 0.0, 1.0), (_Shininess * 128.0))) * _SpecStrength) * (1.0 - clamp ((sqrt(dot (tmpvar_3, tmpvar_3)) / _SpecRange), 0.0, 1.0))).xyzz;
                tmpvar_1 = tmpvar_4;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                o.uv1 = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
                o.uv2 = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.w = tmpvar_2.w;
                c_1.xyz = (tmpvar_2.xyz + (i.uv2.xyz * tmpvar_2.w));
                c_1.xyz = (c_1.xyz * (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz));
                return c_1;
            }
            ENDCG
        }
    }
}