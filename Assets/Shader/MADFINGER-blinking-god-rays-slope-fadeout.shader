// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'glstate_matrix_modelview0' with 'UNITY_MATRIX_MV'

Shader "MADFINGER/Transparent/Blinking GodRays - slope fadeout" {
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

            float _FadeOutDistNear;
            float _FadeOutDistFar;
            float _Multiplier;
            float _Bias;
            float _TimeOnDuration;
            float _TimeOffDuration;
            float _BlinkingTimeOffsScale;
            float _NoiseAmount;
            float4 _Color;

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
                float4 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 tmpvar_1;
                tmpvar_1 = normalize(v.normal);
                float4 tmpvar_2;
                float tmpvar_3;
                tmpvar_3 = (_Time.y + (_BlinkingTimeOffsScale * v.color.z));
                float3 tmpvar_4;
                tmpvar_4 = mul(UNITY_MATRIX_MV, v.vertex).xyz;
                float tmpvar_5;
                tmpvar_5 = sqrt(dot (tmpvar_4, tmpvar_4));
                float tmpvar_6;
                tmpvar_6 = clamp ((tmpvar_5 / _FadeOutDistNear), 0.0, 1.0);
                float tmpvar_7;
                tmpvar_7 = (1.0 - clamp ((max ((tmpvar_5 - _FadeOutDistFar), 0.0) * 0.2), 0.0, 1.0));
                float y_8;
                y_8 = (_TimeOnDuration + _TimeOffDuration);
                float tmpvar_9;
                tmpvar_9 = (tmpvar_3 / y_8);
                float tmpvar_10;
                tmpvar_10 = (frac(abs(tmpvar_9)) * y_8);
                float tmpvar_11;
                if ((tmpvar_9 >= 0.0)) {
                    tmpvar_11 = tmpvar_10;
                } else {
                    tmpvar_11 = -(tmpvar_10);
                };
                float t_12;
                t_12 = max (min ((tmpvar_11 / (_TimeOnDuration * 0.25)), 1.0), 0.0);
                float edge0_13;
                edge0_13 = (_TimeOnDuration * 0.75);
                float t_14;
                t_14 = max (min (((tmpvar_11 - edge0_13) / (_TimeOnDuration - edge0_13)), 1.0), 0.0);
                float tmpvar_15;
                tmpvar_15 = ((t_12 * (t_12 * (3.0 - (2.0 * t_12)))) * (1.0 - (t_14 * (t_14 * (3.0 - (2.0 * t_14))))));
                float tmpvar_16;
                tmpvar_16 = (tmpvar_3 * (6.28319 / _TimeOnDuration));
                float tmpvar_17;
                tmpvar_17 = ((_NoiseAmount * (sin(tmpvar_16) * ((0.5 * cos(((tmpvar_16 * 0.6366) + 56.7272))) + 0.5))) + (1.0 - _NoiseAmount));
                float3x3 tmpvar_18;
                tmpvar_18[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_18[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_18[2] = unity_ObjectToWorld[2].xyz;
                float tmpvar_19;
                tmpvar_19 = clamp (abs(dot (normalize(mul(tmpvar_18, tmpvar_1)), normalize((_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz)))), 0.0, 1.0);
                float tmpvar_20;
                if ((_NoiseAmount < 0.01)) {
                    tmpvar_20 = tmpvar_15;
                } else {
                    tmpvar_20 = tmpvar_17;
                };
                float tmpvar_21;
                tmpvar_21 = (tmpvar_6 * tmpvar_6);
                float4 tmpvar_22;
                tmpvar_22 = ((((((tmpvar_21 * tmpvar_21) * (tmpvar_7 * tmpvar_7)) * _Color) * _Multiplier) * (tmpvar_20 + _Bias)) * tmpvar_19);
                tmpvar_2 = tmpvar_22;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = v.uv.xy;
                o.uv1 = tmpvar_2;

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