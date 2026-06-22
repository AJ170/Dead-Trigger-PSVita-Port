// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Environment/Dual diffuse with lightmap" {
	Properties {
		_MainTex0 ("Base layer0 (RGB)", 2D) = "white" {}
		_MainTex1 ("Base layer1 (RGB)", 2D) = "white" {}
	}
	SubShader { 
		LOD 100
		Tags { "RenderType"="Opaque" }
		Pass {
			Tags { "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _MainTex0_ST;
            float4 _MainTex1_ST;

            sampler2D _MainTex0;
            sampler2D _MainTex1;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                tmpvar_1.xy = ((v.uv.xy * _MainTex0_ST.xy) + _MainTex0_ST.zw);
                tmpvar_1.zw = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;
                o.uv1 = ((v.uv.xy * _MainTex1_ST.xy) + _MainTex1_ST.zw);
                o.color = v.color;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 res_1;
                float4 tmpvar_2;
                tmpvar_2 = lerp (tex2D (_MainTex0, i.uv.xy), tex2D (_MainTex1, i.uv1), i.color.wwww);
                res_1.w = tmpvar_2.w;
                res_1.xyz = (tmpvar_2.xyz * (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv.zw).xyz));
                return res_1;
            }
            ENDCG
        }
    }
}