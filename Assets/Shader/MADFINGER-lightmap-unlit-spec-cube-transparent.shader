// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Environment/Cubemap specular + Lightmap - transparent" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_TransparencyMaskTex ("Transparency Mask (RGB - multiply, A - blend control", 2D) = "white" {}
		_SpecCubeTex ("SpecCube", CUBE) = "black" {}
		_SpecularStrength ("Specular strength", Float) = 1
		_ScrollingSpeed ("Scrolling speed", Vector) = (0,0,0,0)
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

            float4 _ScrollingSpeed;

            sampler2D _MainTex;
            sampler2D _TransparencyMaskTex;

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
                o.uv = (v.uv + frac((_ScrollingSpeed * _Time.y))).xy;
                o.uv1 = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.w = tmpvar_2.w;
                float4 tmpvar_3;
                tmpvar_3 = tex2D (_TransparencyMaskTex, i.uv);
                c_1.xyz = (tmpvar_2.xyz * tmpvar_3.xyz);
                c_1.xyz = (c_1.xyz * (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz));
                c_1.w = tmpvar_3.w;
                return c_1;
            }
            ENDCG
        }
    }
}