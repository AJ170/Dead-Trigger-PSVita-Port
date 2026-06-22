// Upgrade NOTE: replaced 'glstate_matrix_modelview0' with 'UNITY_MATRIX_MV'
// Upgrade NOTE: replaced 'glstate_matrix_projection' with 'UNITY_MATRIX_P'

Shader "MADFINGER/Transparent/Blinking GodRays FPV" {
    Properties {
        _MainTex ("Base texture", 2D) = "white" {}
        _FadeOutDistNear ("Near fadeout dist", Float) = 10
        _FadeOutDistFar ("Far fadeout dist", Float) = 10000
        _Multiplier ("Color multiplier", Float) = 1
        _Bias ("Bias", Float) = 0
        _TimeOnDuration ("ON duration", Float) = 0.5
        _TimeOffDuration ("OFF duration", Float) = 0.5
        _BlinkingTimeOffsScale ("Blinking time offset scale (seconds)", Float) = 5
        _SizeGrowStartDist ("Size grow start dist", Float) = 5
        _SizeGrowEndDist ("Size grow end dist", Float) = 50
        _MaxGrowSize ("Max grow size", Float) = 2.5
        _NoiseAmount ("Noise amount (when zero, pulse wave is used)", Range(0,0.5)) = 0
        _Color ("Color", Color) = (1,1,1,1)
        _Params ("x - FPV proj", Vector) = (1,0,0,0)
    }
    SubShader { 
        LOD 100
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
        Pass {
            Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
            ZWrite Off
            Cull Off
            Fog {
                Color (0,0,0,0)
            }
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _FadeOutDistNear;
            float _FadeOutDistFar;
            float _Multiplier;
            float _Bias;
            float _TimeOnDuration;
            float _TimeOffDuration;
            float _BlinkingTimeOffsScale;
            float _NoiseAmount;
            float4 _Color;
            float4 _MainTex_ST;
            float4 _ProjParams;
            float4 _Params;

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
                float4 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4x4 projTM_1;
                float4 tmpvar_2;
                float4 tmpvar_3;
                float tmpvar_4;
                tmpvar_4 = (_Time.y + (_BlinkingTimeOffsScale * v.color.z));
                float3 tmpvar_5;
                tmpvar_5 = (UnityObjectToViewPos(v.vertex)).xyz;
                float tmpvar_6;
                tmpvar_6 = sqrt(dot (tmpvar_5, tmpvar_5));
                float tmpvar_7;
                tmpvar_7 = clamp ((tmpvar_6 / _FadeOutDistNear), 0.0, 1.0);
                float tmpvar_8;
                tmpvar_8 = (1.0 - clamp ((max ((tmpvar_6 - _FadeOutDistFar), 0.0) * 0.2), 0.0, 1.0));
                float y_9;
                y_9 = (_TimeOnDuration + _TimeOffDuration);
                float tmpvar_10;
                tmpvar_10 = (tmpvar_4 / y_9);
                float tmpvar_11;
                tmpvar_11 = (frac(abs(tmpvar_10)) * y_9);
                float tmpvar_12;
                if ((tmpvar_10 >= 0.0)) {
                    tmpvar_12 = tmpvar_11;
                } else {
                    tmpvar_12 = -(tmpvar_11);
                };
                float t_13;
                t_13 = max (min ((tmpvar_12 / (_TimeOnDuration * 0.25)), 1.0), 0.0);
                float edge0_14;
                edge0_14 = (_TimeOnDuration * 0.75);
                float t_15;
                t_15 = max (min (((tmpvar_12 - edge0_14) / (_TimeOnDuration - edge0_14)), 1.0), 0.0);
                float tmpvar_16;
                tmpvar_16 = ((t_13 * (t_13 * (3.0 - (2.0 * t_13)))) * (1.0 - (t_15 * (t_15 * (3.0 - (2.0 * t_15))))));
                float tmpvar_17;
                tmpvar_17 = (tmpvar_4 * (6.28319 / _TimeOnDuration));
                float tmpvar_18;
                tmpvar_18 = ((_NoiseAmount * (sin(tmpvar_17) * ((0.5 * cos(((tmpvar_17 * 0.6366) + 56.7272))) + 0.5))) + (1.0 - _NoiseAmount));
                float tmpvar_19;
                if ((_NoiseAmount < 0.01)) {
                    tmpvar_19 = tmpvar_16;
                } else {
                    tmpvar_19 = tmpvar_18;
                };
                float tmpvar_20;
                tmpvar_20 = (tmpvar_19 + _Bias);
                float tmpvar_21;
                tmpvar_21 = (tmpvar_7 * tmpvar_7);
                float tmpvar_22;
                tmpvar_22 = ((tmpvar_21 * tmpvar_21) * (tmpvar_8 * tmpvar_8));
                projTM_1 = UNITY_MATRIX_P;
                float4 tmpvar_23;
                if ((_Params.x > 0.0)) {
                    tmpvar_23 = _ProjParams;
                } else {
                    tmpvar_23 = float4(1.0, 1.0, 1.0, 0.0);
                };
                projTM_1[0] = UNITY_MATRIX_P[0]; projTM_1[0].x = (UNITY_MATRIX_P[0].x * tmpvar_23.x);
                projTM_1[1] = projTM_1[1]; projTM_1[1].y = (projTM_1[1].y * tmpvar_23.y);
                float4 tmpvar_24;
                tmpvar_24.w = 1.0;
                tmpvar_24.xyz = tmpvar_5;
                float4 tmpvar_25;
                tmpvar_25 = mul(projTM_1, tmpvar_24);
                tmpvar_2.xyw = tmpvar_25.xyw;
                tmpvar_2.z = (tmpvar_25.z * tmpvar_23.z);
                tmpvar_2.z = (tmpvar_2.z + (tmpvar_23.w * tmpvar_25.w));
                float4 tmpvar_26;
                tmpvar_26 = (((tmpvar_22 * _Color) * _Multiplier) * tmpvar_20);
                tmpvar_3 = tmpvar_26;
                o.pos = tmpvar_2;
                o.uv = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                o.uv1 = tmpvar_3;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = (tex2D (_MainTex, i.uv) * i.uv1);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}