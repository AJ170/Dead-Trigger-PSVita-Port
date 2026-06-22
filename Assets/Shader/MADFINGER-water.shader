// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "MADFINGER/FX/Water" {
	Properties {
		_EnvTex ("Cube env tex", CUBE) = "black" {}
		_Normals ("Normals", 2D) = "bump" {}
		_Params ("Fresnel bias - x, Fresnel pow - y, Transparency bias - z", Vector) = (0,10,0.2,0)
		_Params2 ("Bumps tiling - x, Bumps scroll speed", Vector) = (0.25,0.1,0,0)
		_DeepColor ("Deep color", Color) = (0,1,0,1)
		_ShallowColor ("Shallow color", Color) = (1,0,0,1)
	}
	SubShader { 
		LOD 100
		Tags { "QUEUE"="Transparent-10" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Transparent-10" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Fog { Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Params;
            float4 _DeepColor;
            float4 _ShallowColor;

            samplerCUBE _EnvTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 color1 : COLOR1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 tmpvar_1;
                float4 tmpvar_2;
                float4 tmpvar_3;
                float3x3 tmpvar_4;
                tmpvar_4[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_4[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_4[2] = unity_ObjectToWorld[2].xyz;
                float3 tmpvar_5;
                tmpvar_5 = normalize(mul(tmpvar_4, normalize(v.normal)));
                float3 tmpvar_6;
                tmpvar_6 = normalize((_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz));
                float3 tmpvar_7;
                float3 i_8;
                i_8 = -(tmpvar_6);
                tmpvar_7 = (i_8 - (2.0 * (dot (tmpvar_5, i_8) * tmpvar_5)));
                float tmpvar_9;
                tmpvar_9 = (1.0 - clamp (dot (tmpvar_6, tmpvar_5), 0.0, 1.0));
                float tmpvar_10;
                tmpvar_10 = clamp ((_Params.x + ((1.0 - _Params.x) * pow (tmpvar_9, _Params.y))), 0.0, 1.0);
                tmpvar_1.yz = tmpvar_7.yz;
                tmpvar_1.x = -(tmpvar_7.x);
                float4 tmpvar_11;
                tmpvar_11.xyz = lerp (_DeepColor, _ShallowColor, (tmpvar_9)).xyz;
                tmpvar_11.w = tmpvar_10;
                tmpvar_2 = tmpvar_11;
                float4 tmpvar_12;
                tmpvar_12 = ((tmpvar_10 + _Params.z));
                tmpvar_3 = tmpvar_12;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = tmpvar_1;
                o.color = tmpvar_2;
                o.color1 = tmpvar_3;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float3 refl_1;
                float4 res_2;
                refl_1 = i.uv;
                res_2.w = i.color1.w;
                res_2.xyz = lerp (i.color.xyz, texCUBE (_EnvTex, refl_1).xyz, i.color.www);
                return res_2;
            }
            ENDCG
        }
    }
}