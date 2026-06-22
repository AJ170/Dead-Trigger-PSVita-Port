Shader "MADFINGER/Environment/Cubemap specular + Custom Lightmap + fake bump" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_CustomLightmap ("Custom lightmap", 2D) = "white" {}
		_SpecCubeTex ("SpecCube", CUBE) = "black" {}
		_SpecularStrength ("Specular strength weights", Vector) = (0,0,0,2)
		_ScrollingSpeed ("Scrolling speed", Vector) = (0,0,0,0)
		_Params ("Bumpiness - x", Vector) = (2,0,0,0)
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
            float4 _Params;

            sampler2D _MainTex;
            sampler2D _CustomLightmap;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                tmpvar_1.xy = (v.uv + frac((_ScrollingSpeed * _Time.y))).xy;
                tmpvar_1.zw = (float2(2.0, 1.0) * _Params.x);
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;
                o.uv1 = v.uv1.xy;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv.xy);
                c_1.w = tmpvar_2.w;
                c_1.xyz = (tmpvar_2.xyz * tex2D (_CustomLightmap, i.uv1).xyz);
                return c_1;
            }
            ENDCG
        }
    }
}