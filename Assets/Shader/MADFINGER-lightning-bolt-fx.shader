Shader "MADFINGER/FX/Lightning bolt FX" {
    Properties {
        _MainTex ("Main tex", 2D) = "white" {}
        _Duration ("Duration (x - ON, y - OFF)", Vector) = (0.5,2,0,0)
        _Amplitude ("Amplitude (x - horizontal, y - vertical)", Vector) = (0.2,0.01,0,0)
        _NoiseFreqs ("NoiseFreqs", Vector) = (4,8,16,32)
        _NoiseSpeeds ("NoiseSpeeds", Vector) = (3.2,2.3,0.5,1)
        _NoiseAmps ("NoiseAmps", Vector) = (2,1,0.5,0.125)
        _OtherParams ("x - line width, y - inv wave speed, z - aspect ratio", Vector) = (0.2,0.025,0,0)
        _Color ("Color", Color) = (0.7,0.8,1.3,1)
    }
    SubShader { 
        LOD 100
        Tags { "QUEUE"="Transparent" }
        Pass {
            Tags { "QUEUE"="Transparent" }
            ZWrite Off
            Cull Off
            Fog { Mode Off }
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Duration;
            float4 _Amplitude;
            float4 _NoiseFreqs;
            float4 _NoiseSpeeds;
            float4 _NoiseAmps;
            float4 _Color;
            float4 _OtherParams;



            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 tmpvar_1;
                tmpvar_1 = normalize(v.normal);
                float4 tmpvar_2;
                tmpvar_2 = v.vertex;
                float2 offsDir2D_3;
                float4 sdir_4;
                float4 tmpvar_5;
                float4 tmpvar_6;
                float3 tmpvar_7;
                tmpvar_7 = normalize(tmpvar_1);
                float tmpvar_8;
                tmpvar_8 = abs(tmpvar_7.y);
                float3 tmpvar_9;
                if ((tmpvar_8 > 0.999)) {
                    tmpvar_9 = float3(0.0, 0.0, 1.0);
                } else {
                    tmpvar_9 = float3(0.0, 1.0, 0.0);
                };
                float3 tmpvar_10;
                tmpvar_10 = normalize(((tmpvar_9.yzx * tmpvar_7.zxy) - (tmpvar_9.zxy * tmpvar_7.yzx)));
                float3 tmpvar_11;
                tmpvar_11 = ((tmpvar_7.yzx * tmpvar_10.zxy) - (tmpvar_7.zxy * tmpvar_10.yzx));
                float tmpvar_12;
                tmpvar_12 = (v.uv.x * 0.2);
                float tmpvar_13;
                tmpvar_13 = ((_Time.y * 0.5) + (v.uv1.x * 536.375));
                float4 tmpvar_14;
                tmpvar_14 = ((frac(((12.4512 * ((v.uv1.x * 63.2771))) + 0.5)) * 2.0) - 1.0);
                float4 tmpvar_15;
                tmpvar_15 = (float4(0.7, 0.7, 0.7, 0.7) + (frac((175034.0 * (tmpvar_14 - (tmpvar_14 * abs(tmpvar_14))))) * 0.3));
                float tmpvar_16;
                tmpvar_16 = (_Duration.x * tmpvar_15.x);
                float y_17;
                y_17 = (tmpvar_16 + (_Duration.y * tmpvar_15.y));
                float tmpvar_18;
                tmpvar_18 = ((tmpvar_13 - (tmpvar_12 * _OtherParams.y)) / y_17);
                float tmpvar_19;
                tmpvar_19 = (frac(abs(tmpvar_18)) * y_17);
                float tmpvar_20;
                if ((tmpvar_18 >= 0.0)) {
                    tmpvar_20 = tmpvar_19;
                } else {
                    tmpvar_20 = -(tmpvar_19);
                };
                float t_21;
                t_21 = max (min ((tmpvar_20 / (tmpvar_16 * 0.1)), 1.0), 0.0);
                float edge0_22;
                edge0_22 = (tmpvar_16 * 0.9);
                float t_23;
                t_23 = max (min (((tmpvar_20 - edge0_22) / (tmpvar_16 - edge0_22)), 1.0), 0.0);
                float tmpvar_24;
                tmpvar_24 = ((t_21 * (t_21 * (3.0 - (2.0 * t_21)))) * (1.0 - (t_23 * (t_23 * (3.0 - (2.0 * t_23))))));
                float2 offs_25;
                float4 tmpvar_26;
                tmpvar_26 = ((((tmpvar_12) + float4(183.818, 1041.63, 3860.17, 5575.81)) + (tmpvar_13 * _NoiseSpeeds)) * _NoiseFreqs);
                float4 tmpvar_27;
                tmpvar_27 = ((((tmpvar_12) + float4(5575.81, 183.818, 1041.63, 3860.17)) + (tmpvar_13 * _NoiseSpeeds)) * _NoiseFreqs);
                float4 tmpvar_28;
                tmpvar_28 = frac(tmpvar_26);
                float4 tmpvar_29;
                tmpvar_29 = ((frac(((12.4512 * (tmpvar_26 - tmpvar_28)) + 0.5)) * 2.0) - 1.0);
                float4 tmpvar_30;
                tmpvar_30 = ((frac(((12.4512 * ((tmpvar_26 - tmpvar_28) + 1.0)) + 0.5)) * 2.0) - 1.0);
                float4 tmpvar_31;
                tmpvar_31 = frac(tmpvar_27);
                float4 tmpvar_32;
                tmpvar_32 = ((frac(((12.4512 * (tmpvar_27 - tmpvar_31)) + 0.5)) * 2.0) - 1.0);
                float4 tmpvar_33;
                tmpvar_33 = ((frac(((12.4512 * ((tmpvar_27 - tmpvar_31) + 1.0)) + 0.5)) * 2.0) - 1.0);
                offs_25.x = dot (((lerp (frac((175034.0 * (tmpvar_29 - (tmpvar_29 * abs(tmpvar_29))))), frac((175034.0 * (tmpvar_30 - (tmpvar_30 * abs(tmpvar_30))))), ((tmpvar_28 * tmpvar_28) * (float4(3.0, 3.0, 3.0, 3.0) - (2.0 * tmpvar_28)))) * 2.0) - 1.0), _NoiseAmps);
                offs_25.y = dot (((lerp (frac((175034.0 * (tmpvar_32 - (tmpvar_32 * abs(tmpvar_32))))), frac((175034.0 * (tmpvar_33 - (tmpvar_33 * abs(tmpvar_33))))), ((tmpvar_31 * tmpvar_31) * (float4(3.0, 3.0, 3.0, 3.0) - (2.0 * tmpvar_31)))) * 2.0) - 1.0), _NoiseAmps);
                float2 tmpvar_34;
                tmpvar_34 = ((offs_25 * _Amplitude.xy) * tmpvar_24);
                tmpvar_2.xyz = ((v.vertex.xyz + (tmpvar_10 * tmpvar_34.x)) + (tmpvar_11 * tmpvar_34.y));
                float4 tmpvar_35;
                tmpvar_35.w = 1.0;
                tmpvar_35.xyz = tmpvar_2.xyz;
                float4 tmpvar_36;
                tmpvar_36 = (UnityObjectToClipPos(tmpvar_35));
                float4 tmpvar_37;
                tmpvar_37.w = 1.0;
                tmpvar_37.xyz = (tmpvar_2.xyz + tmpvar_1);
                float4 tmpvar_38;
                tmpvar_38 = (UnityObjectToClipPos(tmpvar_37));
                sdir_4.xy = normalize(((tmpvar_38.xy / tmpvar_38.w) - (tmpvar_36.xy / tmpvar_36.w)));
                float4 tmpvar_39;
                tmpvar_39 = (UnityObjectToClipPos(tmpvar_2));
                tmpvar_5.zw = tmpvar_39.zw;
                offsDir2D_3.x = 1.0;
                offsDir2D_3.y = (-1.0 * _OtherParams.z);
                tmpvar_5.xy = (tmpvar_39.xy + ((((sdir_4.yx * offsDir2D_3) * _OtherParams.x) * v.uv.y) * tmpvar_24));
                float4 tmpvar_40;
                tmpvar_40.xyz = (((_Color.xyz * tmpvar_24) * clamp ((tmpvar_39.w - 1.0), 0.0, 1.0)) * 2.0);
                tmpvar_40.w = clamp ((1.0 - pow ((2.0 * abs((0.5 - v.uv1.y))), 8.0)), 0.0, 1.0);
                tmpvar_6 = tmpvar_40;
                o.pos = tmpvar_5;
                o.uv = v.uv.xy;
                o.uv1 = tmpvar_6;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float c_1;
                float tmpvar_2;
                tmpvar_2 = ((1.0 - abs(i.uv.y)) * i.uv1.w);
                c_1 = tmpvar_2;
                float4 tmpvar_3;
                tmpvar_3 = ((c_1 * c_1) * i.uv1);
                return tmpvar_3;
            }
            ENDCG
        }
    }
}