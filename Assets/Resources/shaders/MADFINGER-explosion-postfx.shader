Shader "MADFINGER/PostFX/ExplosionFX" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "" {}
        _UVOffsAndAspectScale ("UVOffsAndAspectScale", Vector) = (0,0,0,0)
        _Wave0ParamSet0 ("Wave0ParamSet0", Vector) = (0,0,0,0)
        _Wave0ParamSet1 ("Wave0ParamSet1", Vector) = (0,0,0,0)
        _Wave1ParamSet0 ("Wave1ParamSet0", Vector) = (0,0,0,0)
        _Wave1ParamSet1 ("Wave1ParamSet1", Vector) = (0,0,0,0)
        _Wave2ParamSet0 ("Wave2ParamSet0", Vector) = (0,0,0,0)
        _Wave2ParamSet1 ("Wave2ParamSet1", Vector) = (0,0,0,0)
        _Wave3ParamSet0 ("Wave3ParamSet0", Vector) = (0,0,0,0)
        _Wave3ParamSet1 ("Wave3ParamSet1", Vector) = (0,0,0,0)
        _Color0 ("Color0", Color) = (1,1,1,0)
        _Color1 ("Color1", Color) = (0.5,0.3,0,1)
        _Params ("Params", Vector) = (0,0,0,0)
    }
    SubShader { 
        Pass {
            ZTest Always
            ZWrite Off
            Cull Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _UVOffsAndAspectScale;
            float4 _Wave0ParamSet0;
            float4 _Wave0ParamSet1;
            float4 _Wave1ParamSet0;
            float4 _Wave1ParamSet1;
            float4 _Wave2ParamSet0;
            float4 _Wave2ParamSet1;
            float4 _Color0;
            float4 _Color1;
            float4 _Params;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
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

                float4 tmpvar_1;
                float2 tmpvar_2;
                float4 tmpvar_3;
                float2 tmpvar_4;
                float2 tmpvar_5;
                tmpvar_5 = ((v.vertex.xy - _Wave0ParamSet0.xy) * _UVOffsAndAspectScale.zw);
                float tmpvar_6;
                tmpvar_6 = sqrt(dot (tmpvar_5, tmpvar_5));
                float time_7;
                time_7 = (max ((_Wave0ParamSet1.z - (tmpvar_6 / _Wave0ParamSet1.y)), 0.0) * _Wave0ParamSet0.w);
                float tmpvar_8;
                tmpvar_8 = ((sin(time_7) * (1.0/((1.0 + (time_7 * time_7))))) * _Wave0ParamSet0.z);
                float tmpvar_9;
                tmpvar_9 = clamp ((tmpvar_6 * _Wave0ParamSet1.x), 0.0, 1.0);
                float tmpvar_10;
                tmpvar_10 = (max (_Wave0ParamSet1.z, 0.0) * 16.25);
                float tmpvar_11;
                tmpvar_11 = ((tmpvar_10 * exp((1.0 - tmpvar_10))) + 0.0001);
                float tmpvar_12;
                tmpvar_12 = (1.0/(_Wave0ParamSet1.x));
                float tmpvar_13;
                if ((tmpvar_12 > 0.65)) {
                    tmpvar_13 = (tmpvar_12 * 2.0);
                } else {
                    tmpvar_13 = tmpvar_12;
                };
                float tmpvar_14;
                tmpvar_14 = (1.0 - clamp ((tmpvar_6 / (tmpvar_13 * tmpvar_11)), 0.0, 1.0));
                float4 tmpvar_15;
                tmpvar_15 = ((((((tmpvar_14 * tmpvar_14) * _Wave0ParamSet1.x) * lerp (_Color1, _Color0, tmpvar_11.xxxx)) * 1.5) * tmpvar_11) * _Params.x);
                tmpvar_4 = ((tmpvar_8 * (tmpvar_5 / tmpvar_6)) * (1.0 - (tmpvar_9 * tmpvar_9)));
                float2 tmpvar_16;
                float2 tmpvar_17;
                tmpvar_17 = ((v.vertex.xy - _Wave1ParamSet0.xy) * _UVOffsAndAspectScale.zw);
                float tmpvar_18;
                tmpvar_18 = sqrt(dot (tmpvar_17, tmpvar_17));
                float time_19;
                time_19 = (max ((_Wave1ParamSet1.z - (tmpvar_18 / _Wave1ParamSet1.y)), 0.0) * _Wave1ParamSet0.w);
                float tmpvar_20;
                tmpvar_20 = ((sin(time_19) * (1.0/((1.0 + (time_19 * time_19))))) * _Wave1ParamSet0.z);
                float tmpvar_21;
                tmpvar_21 = clamp ((tmpvar_18 * _Wave1ParamSet1.x), 0.0, 1.0);
                float tmpvar_22;
                tmpvar_22 = (max (_Wave1ParamSet1.z, 0.0) * 16.25);
                float tmpvar_23;
                tmpvar_23 = ((tmpvar_22 * exp((1.0 - tmpvar_22))) + 0.0001);
                float tmpvar_24;
                tmpvar_24 = (1.0/(_Wave1ParamSet1.x));
                float tmpvar_25;
                if ((tmpvar_24 > 0.65)) {
                    tmpvar_25 = (tmpvar_24 * 2.0);
                } else {
                    tmpvar_25 = tmpvar_24;
                };
                float tmpvar_26;
                tmpvar_26 = (1.0 - clamp ((tmpvar_18 / (tmpvar_25 * tmpvar_23)), 0.0, 1.0));
                float4 tmpvar_27;
                tmpvar_27 = ((((((tmpvar_26 * tmpvar_26) * _Wave1ParamSet1.x) * lerp (_Color1, _Color0, tmpvar_23.xxxx)) * 1.5) * tmpvar_23) * _Params.x);
                tmpvar_16 = ((tmpvar_20 * (tmpvar_17 / tmpvar_18)) * (1.0 - (tmpvar_21 * tmpvar_21)));
                float2 tmpvar_28;
                tmpvar_28 = ((v.vertex.xy - _Wave2ParamSet0.xy) * _UVOffsAndAspectScale.zw);
                float tmpvar_29;
                tmpvar_29 = sqrt(dot (tmpvar_28, tmpvar_28));
                float time_30;
                time_30 = (max ((_Wave2ParamSet1.z - (tmpvar_29 / _Wave2ParamSet1.y)), 0.0) * _Wave2ParamSet0.w);
                float tmpvar_31;
                tmpvar_31 = ((sin(time_30) * (1.0/((1.0 + (time_30 * time_30))))) * _Wave2ParamSet0.z);
                float tmpvar_32;
                tmpvar_32 = clamp ((tmpvar_29 * _Wave2ParamSet1.x), 0.0, 1.0);
                float tmpvar_33;
                tmpvar_33 = (max (_Wave2ParamSet1.z, 0.0) * 16.25);
                float tmpvar_34;
                tmpvar_34 = ((tmpvar_33 * exp((1.0 - tmpvar_33))) + 0.0001);
                float tmpvar_35;
                tmpvar_35 = (1.0/(_Wave2ParamSet1.x));
                float tmpvar_36;
                if ((tmpvar_35 > 0.65)) {
                    tmpvar_36 = (tmpvar_35 * 2.0);
                } else {
                    tmpvar_36 = tmpvar_35;
                };
                float tmpvar_37;
                tmpvar_37 = (1.0 - clamp ((tmpvar_29 / (tmpvar_36 * tmpvar_34)), 0.0, 1.0));
                float4 tmpvar_38;
                tmpvar_38 = ((((((tmpvar_37 * tmpvar_37) * _Wave2ParamSet1.x) * lerp (_Color1, _Color0, tmpvar_34.xxxx)) * 1.5) * tmpvar_34) * _Params.x);
                float4 tmpvar_39;
                tmpvar_39.zw = float2(0.0, 1.0);
                tmpvar_39.xy = ((v.vertex.xy * 2.0) - float2(1.0, 1.0));
                tmpvar_1 = tmpvar_39;
                tmpvar_2 = ((v.vertex.xy + _UVOffsAndAspectScale.xy) + ((tmpvar_4 + tmpvar_16) + ((tmpvar_31 * (tmpvar_28 / tmpvar_29)) * (1.0 - (tmpvar_32 * tmpvar_32)))));
                int tmpvar_40;
                if ((_UVOffsAndAspectScale.y < 0.0)) {
                    tmpvar_40 = -1;
                } else {
                    tmpvar_40 = 1;
                };
                tmpvar_1.y = (tmpvar_39.y * float(tmpvar_40));
                float4 tmpvar_41;
                tmpvar_41 = ((tmpvar_15 + tmpvar_27) + tmpvar_38);
                tmpvar_3 = tmpvar_41;
                o.pos = tmpvar_1;
                o.uv = tmpvar_2;
                o.color = tmpvar_3;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = (tex2D (_MainTex, i.uv) + i.color);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}