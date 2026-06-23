Shader "MADFINGER/Particles/Additive TwoSided FPV" {
	Properties {
		_MainTex ("Texture", 2D) = "black" {}
		_TintColor ("Color", Color) = (1,1,1,1)
	}
	SubShader { 
		Tags {  "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		Pass {
			Tags {  "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
			ZWrite Off
			Cull Off
			Fog {
				Color (0,0,0,0)
			}
			Blend SrcAlpha One
			ColorMask RGB

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _TintColor;
            float4 _ProjParams;
            float4 _MainTex_ST;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {               
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = _TintColor;
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = (tex2D (_MainTex, i.uv) * i.color);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}