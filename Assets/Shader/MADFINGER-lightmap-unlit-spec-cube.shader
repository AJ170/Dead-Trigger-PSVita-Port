Shader "MADFINGER/Environment/Cubemap specular + Lightmap" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_SpecCubeTex ("SpecCube", CUBE) = "black" {}
		_SpecularStrength ("Specular strength weights", Vector) = (0,0,0,2)
		_ScrollingSpeed ("Scrolling speed", Vector) = (0,0,0,0)
	}
	SubShader { 
		LOD 100
		Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }
		Pass {
			Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _ScrollingSpeed;
            float4 _MainTex_ST;

            sampler2D _MainTex;

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
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = (((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw) + frac((_ScrollingSpeed * _Time.y)).xy);
                o.uv1 = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
                o.color = v.color;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.w = tmpvar_2.w;
                c_1.xyz = (tmpvar_2.xyz * (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz));
                c_1.xyz = (c_1.xyz * i.color.xyz);
                return c_1;
            }
            ENDCG
        }
    }
}