Shader "MADFINGER/Characters/BRDFLit FX (Supports Backlight)" {
    Properties {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "grey" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
        _BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
        _NoiseTex ("Noise tex", 2D) = "white" {}
        _LightProbesLightingAmount ("Light probes lighting amount", Range(0,1)) = 0.9
        _FXColor ("FXColor", Color) = (0,0.97,0.89,1)
        _TimeOffs ("Time offs", Float) = 0
        _Duration ("Duration", Float) = 2
        _Invert ("Invert", Float) = 0
    }
    SubShader { 
        LOD 400
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
        Pass {
            Name "FORWARD"
            Tags { "LIGHTMODE"="ForwardBase" "SHADOWSUPPORT"="true" "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _TimeOffs;
            float _Duration;
            float _LightProbesLightingAmount;
            float _Invert;
            float _GlobalTime;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;

            sampler2D _BRDFTex;
            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _NoiseTex;
            float4 _FXColor;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float3 uv3 : TEXCOORD3;
                float3 uv4 : TEXCOORD4;
                float3 uv5 : TEXCOORD5;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                tmpvar_1.xyz = normalize(v.tangent.xyz);
                tmpvar_1.w = v.tangent.w;
                float3 tmpvar_2;
                tmpvar_2 = normalize(v.normal);
                float4 tmpvar_3;
                float3 tmpvar_4;
                float tmpvar_5;
                float3 tmpvar_6;
                float3 tmpvar_7;
                float3 tmpvar_8;
                float tmpvar_9;
                float3 SHLighting_10;
                float3x3 tmpvar_11;
                tmpvar_11[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_11[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_11[2] = unity_ObjectToWorld[2].xyz;
                float4 tmpvar_12;
                tmpvar_12.w = 1.0;
                tmpvar_12.xyz = mul(tmpvar_11, tmpvar_2);
                float3 tmpvar_13;
                float4 normal_14;
                normal_14 = tmpvar_12;
                float vC_15;
                float3 x3_16;
                float3 x2_17;
                float3 x1_18;
                float tmpvar_19;
                tmpvar_19 = dot (unity_SHAr, normal_14);
                x1_18.x = tmpvar_19;
                float tmpvar_20;
                tmpvar_20 = dot (unity_SHAg, normal_14);
                x1_18.y = tmpvar_20;
                float tmpvar_21;
                tmpvar_21 = dot (unity_SHAb, normal_14);
                x1_18.z = tmpvar_21;
                float4 tmpvar_22;
                tmpvar_22 = (normal_14.xyzz * normal_14.yzzx);
                float tmpvar_23;
                tmpvar_23 = dot (unity_SHBr, tmpvar_22);
                x2_17.x = tmpvar_23;
                float tmpvar_24;
                tmpvar_24 = dot (unity_SHBg, tmpvar_22);
                x2_17.y = tmpvar_24;
                float tmpvar_25;
                tmpvar_25 = dot (unity_SHBb, tmpvar_22);
                x2_17.z = tmpvar_25;
                float tmpvar_26;
                tmpvar_26 = ((normal_14.x * normal_14.x) - (normal_14.y * normal_14.y));
                vC_15 = tmpvar_26;
                float3 tmpvar_27;
                tmpvar_27 = (unity_SHC.xyz * vC_15);
                x3_16 = tmpvar_27;
                tmpvar_13 = ((x1_18 + x2_17) + x3_16);
                SHLighting_10 = tmpvar_13;
                float tmpvar_28;
                tmpvar_28 = clamp (((_TimeOffs + _GlobalTime) / _Duration), 0.0, 1.0);
                float3 tmpvar_29;
                tmpvar_29 = clamp ((SHLighting_10 + ((1.0 - _LightProbesLightingAmount))), 0.0, 1.0);
                tmpvar_8 = tmpvar_29;
                float tmpvar_30;
                if ((_Invert > 0.0)) {
                    tmpvar_30 = (1.0 - tmpvar_28);
                } else {
                    tmpvar_30 = tmpvar_28;
                };
                tmpvar_9 = tmpvar_30;
                tmpvar_4 = tmpvar_8;
                tmpvar_5 = tmpvar_9;
                tmpvar_3.xy = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                tmpvar_3.zw = ((v.uv.xy * _BumpMap_ST.xy) + _BumpMap_ST.zw);
                float3 tmpvar_31;
                float3 tmpvar_32;
                tmpvar_31 = tmpvar_1.xyz;
                tmpvar_32 = (((tmpvar_2.yzx * tmpvar_1.zxy) - (tmpvar_2.zxy * tmpvar_1.yzx)) * v.tangent.w);
                float3x3 tmpvar_33;
                tmpvar_33[0].x = tmpvar_31.x;
                tmpvar_33[0].y = tmpvar_32.x;
                tmpvar_33[0].z = tmpvar_2.x;
                tmpvar_33[1].x = tmpvar_31.y;
                tmpvar_33[1].y = tmpvar_32.y;
                tmpvar_33[1].z = tmpvar_2.y;
                tmpvar_33[2].x = tmpvar_31.z;
                tmpvar_33[2].y = tmpvar_32.z;
                tmpvar_33[2].z = tmpvar_2.z;
                float3 tmpvar_34;
                tmpvar_34 = mul(tmpvar_33, mul(unity_WorldToObject, _WorldSpaceLightPos0).xyz);
                tmpvar_6 = tmpvar_34;
                float4 tmpvar_35;
                tmpvar_35.w = 1.0;
                tmpvar_35.xyz = _WorldSpaceCameraPos;
                float3 tmpvar_36;
                tmpvar_36 = normalize(mul(tmpvar_33, ((mul(unity_WorldToObject, tmpvar_35).xyz * 1.0) - v.vertex.xyz)));
                tmpvar_7 = tmpvar_36;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = tmpvar_3;
                o.uv1 = tmpvar_4;
                o.uv2 = tmpvar_5;
                o.uv3 = tmpvar_6;
                o.uv4 = float3(0.0, 0.0, 0.0);
                o.uv5 = tmpvar_7;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float3 tmpvar_2;
                float tmpvar_3;
                tmpvar_2 = i.uv1;
                tmpvar_3 = i.uv2;
                float4 tmpvar_4;
                float2 P_5;
                P_5 = (i.uv.xy * 2.0);
                tmpvar_4 = tex2D (_NoiseTex, P_5);
                float4 tmpvar_6;
                tmpvar_6 = tex2D (_MainTex, i.uv.xy);
                float tmpvar_7;
                tmpvar_7 = (1.0 - clamp (((tmpvar_4.x - tmpvar_3) * 4.0), 0.0, 1.0));
                float tmpvar_8;
                tmpvar_8 = (tmpvar_7 * tmpvar_7);
                float3 tmpvar_9;
                tmpvar_9 = (tmpvar_6.xyz * tmpvar_2);
                float3 tmpvar_10;
                tmpvar_10 = ((tex2D (_BumpMap, i.uv.zw).xyz * 2.0) - 1.0);
                float4 c_11;
                float2 tmpvar_12;
                tmpvar_12.x = ((dot (tmpvar_10, i.uv3) * 0.5) + 0.5);
                tmpvar_12.y = dot (tmpvar_10, normalize((i.uv3 + i.uv5)));
                float4 tmpvar_13;
                tmpvar_13 = tex2D (_BRDFTex, tmpvar_12);
                c_11.xyz = ((tmpvar_9 * (tmpvar_13.xyz + (tmpvar_6.w * tmpvar_13.w))) * 2.0);
                c_11.w = float((tmpvar_4.x > tmpvar_3));
                c_1.w = c_11.w;
                c_1.xyz = (c_11.xyz + (tmpvar_9 * i.uv4));
                c_1.xyz = (c_1.xyz + (_FXColor * (tmpvar_8 * tmpvar_8)).xyz);
                return c_1;
            }
            ENDCG
        }
    }
}