Shader "MADFINGER/Environment/Cube env map transparent (Supports LightProbes) FPV" {
    Properties {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _EnvTex ("Cube env tex", CUBE) = "black" {}
        _SHLightingScale ("LightProbe influence scale", Float) = 1
        _FadeoutDist ("Fadeout near (x), far (y)", Vector) = (0.5,1,0,0)
    }
    SubShader { 
        LOD 100
        Tags { "LIGHTMODE"="ForwardBase" "QUEUE"="Transparent" "RenderType"="Transparent" }
        Pass {
            Tags { "LIGHTMODE"="ForwardBase" "QUEUE"="Transparent" "RenderType"="Transparent" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _SHLightingScale;
            float4 _FadeoutDist;
            float4 _ProjParams;
            float4 _MainTex_ST;

            sampler2D _MainTex;

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
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4x4 projTM_1;
                float4 tmpvar_2;
                float4 tmpvar_3;
                float4 tmpvar_4;
                projTM_1 = UNITY_MATRIX_P;
                projTM_1[0] = UNITY_MATRIX_P[0]; projTM_1[0].x = (UNITY_MATRIX_P[0].x * _ProjParams.x);
                projTM_1[1] = projTM_1[1]; projTM_1[1].y = (projTM_1[1].y * _ProjParams.y);
                float4 tmpvar_6;
                tmpvar_6.w = 1.0;
                tmpvar_6.xyz = UnityObjectToViewPos(v.vertex).xyz;
                float4 tmpvar_7;
                tmpvar_7 = mul(projTM_1, tmpvar_6);
                tmpvar_2.xyw = tmpvar_7.xyw;
                tmpvar_2.z = (tmpvar_7.z * _ProjParams.z);
                tmpvar_2.z = (tmpvar_2.z + (_ProjParams.w * tmpvar_7.w));
                float2 tmpvar_8;
                tmpvar_8 = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                float3x3 tmpvar_9;
                tmpvar_9[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_9[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_9[2] = unity_ObjectToWorld[2].xyz;
                float3 tmpvar_10;
                tmpvar_10 = mul(tmpvar_9, normalize(v.normal));
                float tmpvar_11;
                float3 arg0_12;
                arg0_12 = (mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos);
                tmpvar_11 = sqrt(dot (arg0_12, arg0_12));
                float v_13;
                v_13 = tmpvar_11;
                float r0_14;
                r0_14 = _FadeoutDist.x;
                float r1_15;
                r1_15 = _FadeoutDist.y;
                if ((tmpvar_11 < _FadeoutDist.x)) {
                    v_13 = r0_14;
                } else {
                    if ((v_13 > _FadeoutDist.y)) {
                        v_13 = r1_15;
                    };
                };
                float3 i_16;
                i_16 = (mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos);
                tmpvar_3.xyz = (i_16 - (2.0 * (dot (tmpvar_10, i_16) * tmpvar_10)));
                tmpvar_3.x = -(tmpvar_3.x);
                tmpvar_3.w = ((v_13 - _FadeoutDist.x) / (_FadeoutDist.y - _FadeoutDist.x));
                float4 tmpvar_17;
                tmpvar_17.w = 1.0;
                tmpvar_17.xyz = tmpvar_10;
                float3 tmpvar_18;
                float4 normal_19;
                normal_19 = tmpvar_17;
                float vC_20;
                float3 x3_21;
                float3 x2_22;
                float3 x1_23;
                float tmpvar_24;
                tmpvar_24 = dot (unity_SHAr, normal_19);
                x1_23.x = tmpvar_24;
                float tmpvar_25;
                tmpvar_25 = dot (unity_SHAg, normal_19);
                x1_23.y = tmpvar_25;
                float tmpvar_26;
                tmpvar_26 = dot (unity_SHAb, normal_19);
                x1_23.z = tmpvar_26;
                float4 tmpvar_27;
                tmpvar_27 = (normal_19.xyzz * normal_19.yzzx);
                float tmpvar_28;
                tmpvar_28 = dot (unity_SHBr, tmpvar_27);
                x2_22.x = tmpvar_28;
                float tmpvar_29;
                tmpvar_29 = dot (unity_SHBg, tmpvar_27);
                x2_22.y = tmpvar_29;
                float tmpvar_30;
                tmpvar_30 = dot (unity_SHBb, tmpvar_27);
                x2_22.z = tmpvar_30;
                float tmpvar_31;
                tmpvar_31 = ((normal_19.x * normal_19.x) - (normal_19.y * normal_19.y));
                vC_20 = tmpvar_31;
                float3 tmpvar_32;
                tmpvar_32 = (unity_SHC.xyz * vC_20);
                x3_21 = tmpvar_32;
                tmpvar_18 = ((x1_23 + x2_22) + x3_21);
                float4 tmpvar_33;
                tmpvar_33.xyz = (tmpvar_18 * _SHLightingScale);
                tmpvar_33.w = v.color.w;
                tmpvar_4 = tmpvar_33;
                o.pos = tmpvar_2;
                o.uv = tmpvar_8;
                o.uv1 = tmpvar_3;
                o.color = tmpvar_4;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.xyz = (tmpvar_2.xyz * i.color.xyz);
                float tmpvar_3;
                tmpvar_3 = (tmpvar_2.w * i.uv1.w);
                c_1.w = tmpvar_3;
                return c_1;
            }
            ENDCG
        }
    }
}
