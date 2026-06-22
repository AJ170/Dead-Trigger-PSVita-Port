Shader "MADFINGER/Characters/BRDFLit (Supports Backlight) - custom glossingess mask - zombie" {
    Properties {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "grey" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
        _BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
        _LightProbesLightingAmount ("Light probes lighting amount", Range(0,1)) = 0.9
        _SpecularStrength ("Specular strength weights", Vector) = (0,0,0,1)
        _Params ("x = open holes, y = FPV projection", Vector) = (0,0,0,0)
    }
    SubShader { 
        LOD 400
        Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }
        Pass {
            Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _LightProbesLightingAmount;
            float4 _Params;
            float4 _ProjParams;

            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _BRDFTex;
            float4 _SpecularStrength;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 uv1 : TEXCOORD1;
                float3 uv2 : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                tmpvar_1.xyz = normalize(v.tangent.xyz);
                tmpvar_1.w = v.tangent.w;
                float3 tmpvar_2;
                tmpvar_2 = normalize(v.normal);
                float3 SHLighting_3;
                float4 tmpvar_4;
                float3 tmpvar_5;
                float3 tmpvar_6;
                float4 tmpvar_7;
                float4x4 projTM_8;
                projTM_8 = UNITY_MATRIX_P;
                float3x3 tmpvar_9;
                tmpvar_9[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_9[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_9[2] = unity_ObjectToWorld[2].xyz;
                float3 tmpvar_10;
                tmpvar_10 = mul(tmpvar_9, tmpvar_2);
                float3 tmpvar_11;
                tmpvar_11 = UnityObjectToViewPos(v.vertex).xyz;
                float4 tmpvar_12;
                if ((_Params.y > 0.0)) {
                    tmpvar_12 = _ProjParams;
                } else {
                    tmpvar_12 = float4(1.0, 1.0, 1.0, 0.0);
                };
                float3 tmpvar_13;
                if ((v.color.w < _Params.x)) {
                  tmpvar_13 = float3(0.0, 0.0, 0.0);
                } else {
                  tmpvar_13 = tmpvar_11;
                };
                projTM_8[0] = UNITY_MATRIX_P[0]; projTM_8[0].x = (UNITY_MATRIX_P[0].x * tmpvar_12.x);
                projTM_8[1] = projTM_8[1]; projTM_8[1].y = (projTM_8[1].y * tmpvar_12.y);
                float4 tmpvar_14;
                tmpvar_14.w = 1.0;
                tmpvar_14.xyz = tmpvar_13;
                float4 tmpvar_15;
                tmpvar_15 = mul(projTM_8, tmpvar_14);
                tmpvar_4.xyw = tmpvar_15.xyw;
                tmpvar_4.z = (tmpvar_15.z * tmpvar_12.z);
                tmpvar_4.z = (tmpvar_4.z + (tmpvar_12.w * tmpvar_15.w));
                tmpvar_4.z = max (tmpvar_4.z, 0.0);
                float3 tmpvar_16;
                float3 tmpvar_17;
                tmpvar_16 = tmpvar_1.xyz;
                tmpvar_17 = (((tmpvar_2.yzx * tmpvar_1.zxy) - (tmpvar_2.zxy * tmpvar_1.yzx)) * v.tangent.w);
                float3x3 tmpvar_18;
                tmpvar_18[0].x = tmpvar_16.x;
                tmpvar_18[0].y = tmpvar_17.x;
                tmpvar_18[0].z = tmpvar_2.x;
                tmpvar_18[1].x = tmpvar_16.y;
                tmpvar_18[1].y = tmpvar_17.y;
                tmpvar_18[1].z = tmpvar_2.y;
                tmpvar_18[2].x = tmpvar_16.z;
                tmpvar_18[2].y = tmpvar_17.z;
                tmpvar_18[2].z = tmpvar_2.z;
                float3x3 tmpvar_19;
                tmpvar_19[0] = unity_WorldToObject[0].xyz;
                tmpvar_19[1] = unity_WorldToObject[1].xyz;
                tmpvar_19[2] = unity_WorldToObject[2].xyz;
                float3 res_20;
                float3 tmpvar_21;
                tmpvar_21 = (((unity_SHAr.xyz * 0.3) + (unity_SHAg.xyz * 0.59)) + (unity_SHAb.xyz * 0.11));
                res_20 = tmpvar_21;
                float3 tmpvar_22;
                tmpvar_22 = mul(tmpvar_19, res_20);
                float3 tmpvar_23;
                tmpvar_23 = normalize(mul(tmpvar_18, tmpvar_22));
                tmpvar_5 = tmpvar_23;
                float4 tmpvar_24;
                tmpvar_24.w = 1.0;
                tmpvar_24.xyz = _WorldSpaceCameraPos;
                float3 tmpvar_25;
                tmpvar_25 = normalize((normalize(mul(tmpvar_18, ((mul(unity_WorldToObject, tmpvar_24).xyz * 1.0) - v.vertex.xyz))) + tmpvar_5));
                tmpvar_6 = tmpvar_25;
                float4 tmpvar_26;
                tmpvar_26.w = 1.0;
                tmpvar_26.xyz = tmpvar_10;
                float3 tmpvar_27;
                float4 normal_28;
                normal_28 = tmpvar_26;
                float vC_29;
                float3 x3_30;
                float3 x2_31;
                float3 x1_32;
                float tmpvar_33;
                tmpvar_33 = dot (unity_SHAr, normal_28);
                x1_32.x = tmpvar_33;
                float tmpvar_34;
                tmpvar_34 = dot (unity_SHAg, normal_28);
                x1_32.y = tmpvar_34;
                float tmpvar_35;
                tmpvar_35 = dot (unity_SHAb, normal_28);
                x1_32.z = tmpvar_35;
                float4 tmpvar_36;
                tmpvar_36 = (normal_28.xyzz * normal_28.yzzx);
                float tmpvar_37;
                tmpvar_37 = dot (unity_SHBr, tmpvar_36);
                x2_31.x = tmpvar_37;
                float tmpvar_38;
                tmpvar_38 = dot (unity_SHBg, tmpvar_36);
                x2_31.y = tmpvar_38;
                float tmpvar_39;
                tmpvar_39 = dot (unity_SHBb, tmpvar_36);
                x2_31.z = tmpvar_39;
                float tmpvar_40;
                tmpvar_40 = ((normal_28.x * normal_28.x) - (normal_28.y * normal_28.y));
                vC_29 = tmpvar_40;
                float3 tmpvar_41;
                tmpvar_41 = (unity_SHC.xyz * vC_29);
                x3_30 = tmpvar_41;
                tmpvar_27 = ((x1_32 + x2_31) + x3_30);
                SHLighting_3 = tmpvar_27;
                float4 tmpvar_42;
                tmpvar_42 = clamp ((SHLighting_3 + ((1.0 - _LightProbesLightingAmount))), 0.0, 1.0).xyzz;
                tmpvar_7 = tmpvar_42;
                o.pos = tmpvar_4;
                o.uv = v.uv.xy;
                o.uv1 = tmpvar_5;
                o.uv2 = tmpvar_6;
                o.color = tmpvar_7;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float gloss_1;
                float4 c_2;
                float4 tmpvar_3;
                tmpvar_3 = tex2D (_MainTex, i.uv);
                c_2.w = tmpvar_3.w;
                float3 tmpvar_4;
                tmpvar_4 = ((tex2D (_BumpMap, i.uv).xyz * 2.0) - 1.0);
                float tmpvar_5;
                tmpvar_5 = dot (_SpecularStrength, tmpvar_3);
                gloss_1 = tmpvar_5;
                c_2.xyz = (tmpvar_3.xyz * i.color.xyz);
                float2 tmpvar_6;
                tmpvar_6.x = ((dot (tmpvar_4, i.uv1) * 0.5) + 0.5);
                tmpvar_6.y = dot (tmpvar_4, i.uv2);
                float4 tmpvar_7;
                tmpvar_7 = tex2D (_BRDFTex, tmpvar_6);
                c_2.xyz = (c_2.xyz * ((tmpvar_7.xyz + (gloss_1 * tmpvar_7.w)) * 2.0));
                return c_2;
            }
            ENDCG
        }
    }
}