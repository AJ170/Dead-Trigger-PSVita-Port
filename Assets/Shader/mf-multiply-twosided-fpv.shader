Shader "MADFINGER/Particles/Multiply TwoSided FPV" {
	Properties {
		_MainTex ("Texture", 2D) = "black" {}
		_TintColor ("Color", Color) = (1,1,1,1)
	}
	SubShader { 
		Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Cull Off
			Fog {
				Color (0,0,0,0)
			}
			Blend DstColor Zero
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

                float4x4 projTM_1;
                float4 tmpvar_2;
                float4 tmpvar_3;
                projTM_1 = UNITY_MATRIX_P;
                projTM_1[0] = UNITY_MATRIX_P[0]; projTM_1[0].x = (UNITY_MATRIX_P[0].x * _ProjParams.x);
                projTM_1[1] = projTM_1[1]; projTM_1[1].y = (projTM_1[1].y * _ProjParams.y);
                float4 tmpvar_4;
                tmpvar_4.w = 1.0;
                tmpvar_4.xyz = UnityObjectToViewPos(v.vertex).xyz;
                float4 tmpvar_5;
                tmpvar_5 = mul(projTM_1, tmpvar_4);
                tmpvar_2.xyw = tmpvar_5.xyw;
                tmpvar_2.z = (tmpvar_5.z * _ProjParams.z);
                tmpvar_2.z = (tmpvar_2.z + (_ProjParams.w * tmpvar_5.w));
                float4 tmpvar_6;
                tmpvar_6.w = 1.0;
                tmpvar_6.xyz = (v.color.xyz * _TintColor.xyz);
                tmpvar_3 = tmpvar_6;
                o.pos = tmpvar_2;
                o.color = tmpvar_3;
                o.uv = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);

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