Shader "MADFINGER/Environment/Unlit (Supports Lightmap) Transparent" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("HACK: temporary to fix lightmap bouncing light (will be fixed in RC1)", Color) = (1,1,1,1)
	}
	SubShader { 
		LOD 100
		Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

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
                float4 tmpvar_1;
                float4 c_2;
                float4 tmpvar_3;
                tmpvar_3 = tex2D (_MainTex, i.uv);
                c_2 = tmpvar_3;
                float3 tmpvar_4;
                tmpvar_4 = (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz);
                c_2.xyz = (c_2.xyz * tmpvar_4);
                tmpvar_1 = c_2;
                return tmpvar_1;
            }
            ENDCG
        }
    }
}