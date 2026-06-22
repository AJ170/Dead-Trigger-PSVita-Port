Shader "MADFINGER/Environment/Cube env map (Supports LightProbes)" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_EnvTex ("Cube env tex", CUBE) = "black" {}
		_SHLightingScale ("LightProbe influence scale", Float) = 1
		_EnvStrength ("Env strength weights", Vector) = (0,0,0,2)
		_UVScrollSpeed ("UV scroll speed XY", Vector) = (0,0,0,0)
	}
	SubShader { 
		LOD 100
		Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }
		Pass {
			Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _MainTex_ST;
            float _SHLightingScale;
            float4 _UVScrollSpeed;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 tmpvar_1;
                float3x3 tmpvar_2;
                tmpvar_2[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_2[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_2[2] = unity_ObjectToWorld[2].xyz;
                float3 tmpvar_3;
                tmpvar_3 = mul(tmpvar_2, normalize(v.normal));
                float3 tmpvar_4;
                float3 i_5;
                i_5 = (mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos);
                tmpvar_4 = (i_5 - (2.0 * (dot (tmpvar_3, i_5) * tmpvar_3)));
                tmpvar_1.yz = tmpvar_4.yz;
                tmpvar_1.x = -(tmpvar_4.x);
                float4 tmpvar_6;
                tmpvar_6.w = 1.0;
                tmpvar_6.xyz = tmpvar_3;
                float3 tmpvar_7;
                float4 normal_8;
                normal_8 = tmpvar_6;
                float vC_9;
                float3 x3_10;
                float3 x2_11;
                float3 x1_12;
                float tmpvar_13;
                tmpvar_13 = dot (unity_SHAr, normal_8);
                x1_12.x = tmpvar_13;
                float tmpvar_14;
                tmpvar_14 = dot (unity_SHAg, normal_8);
                x1_12.y = tmpvar_14;
                float tmpvar_15;
                tmpvar_15 = dot (unity_SHAb, normal_8);
                x1_12.z = tmpvar_15;
                float4 tmpvar_16;
                tmpvar_16 = (normal_8.xyzz * normal_8.yzzx);
                float tmpvar_17;
                tmpvar_17 = dot (unity_SHBr, tmpvar_16);
                x2_11.x = tmpvar_17;
                float tmpvar_18;
                tmpvar_18 = dot (unity_SHBg, tmpvar_16);
                x2_11.y = tmpvar_18;
                float tmpvar_19;
                tmpvar_19 = dot (unity_SHBb, tmpvar_16);
                x2_11.z = tmpvar_19;
                float tmpvar_20;
                tmpvar_20 = ((normal_8.x * normal_8.x) - (normal_8.y * normal_8.y));
                vC_9 = tmpvar_20;
                float3 tmpvar_21;
                tmpvar_21 = (unity_SHC.xyz * vC_9);
                x3_10 = tmpvar_21;
                tmpvar_7 = ((x1_12 + x2_11) + x3_10);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = (((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw) + frac((_UVScrollSpeed.xy * _Time.xy)));
                o.uv1 = tmpvar_1;
                o.color = ((tmpvar_7 * _SHLightingScale) * v.color.xyz);

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.w = tmpvar_2.w;
                float3 tmpvar_3;
                tmpvar_3 = (tmpvar_2.xyz * i.color);
                c_1.xyz = tmpvar_3;
                return c_1;
            }
            ENDCG
        }
    }
}