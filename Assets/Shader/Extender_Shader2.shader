Shader "__CAPA__/Shader_Extender2" {
    Properties {
        _MainTex ("Base texture", 2D) = "white" {}
        _Multiplier ("Color multiplier", Float) = 1
        _Bias ("Bias", Float) = 0
        _TimeOnDuration ("ON duration", Float) = 0.5
        _TimeOffDuration ("OFF duration", Float) = 0.5
        _BlinkingTimeOffsScale ("Blinking time offset scale (seconds)", Float) = 5
        _NoiseAmount ("Noise amount (when zero, pulse wave is used)", Range(0,0.5)) = 0
        _Color ("Color", Color) = (1,1,1,1)
        _Scaling ("XYZ = direction, Z = MaxDist", Vector) = (0,0,1,1)
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

            float4 _MainTex_ST;
            float _Multiplier;
            float _Bias;
            float _TimeOnDuration;
            float _TimeOffDuration;
            float _BlinkingTimeOffsScale;
            float _NoiseAmount;
            float4 _Color;
            float4 _Scaling;

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

                float4 pos_1;
                float4 tmpvar_2;
                float tmpvar_3;
                tmpvar_3 = (_Time.y + (_BlinkingTimeOffsScale * v.color.z));
                float y_4;
                y_4 = (_TimeOnDuration + _TimeOffDuration);
                float tmpvar_5;
                tmpvar_5 = (tmpvar_3 / y_4);
                float tmpvar_6;
                tmpvar_6 = (frac(abs(tmpvar_5)) * y_4);
                float tmpvar_7;
                if ((tmpvar_5 >= 0.0)) {
                    tmpvar_7 = tmpvar_6;
                } else {
                    tmpvar_7 = -(tmpvar_6);
                };
                float t_8;
                t_8 = max (min ((tmpvar_7 / (_TimeOnDuration * 0.25)), 1.0), 0.0);
                float edge0_9;
                edge0_9 = (_TimeOnDuration * 0.75);
                float t_10;
                t_10 = max (min (((tmpvar_7 - edge0_9) / (_TimeOnDuration - edge0_9)), 1.0), 0.0);
                float tmpvar_11;
                tmpvar_11 = ((t_8 * (t_8 * (3.0 - (2.0 * t_8)))) * (1.0 - (t_10 * (t_10 * (3.0 - (2.0 * t_10))))));
                float tmpvar_12;
                tmpvar_12 = (tmpvar_3 * (6.28319 / _TimeOnDuration));
                float tmpvar_13;
                tmpvar_13 = ((_NoiseAmount * (sin(tmpvar_12) * ((0.5 * cos(((tmpvar_12 * 0.6366) + 56.7272))) + 0.5))) + (1.0 - _NoiseAmount));
                float tmpvar_14;
                if ((_NoiseAmount < 0.01)) {
                    tmpvar_14 = tmpvar_11;
                } else {
                    tmpvar_14 = tmpvar_13;
                };
                float tmpvar_15;
                tmpvar_15 = (tmpvar_14 + _Bias);
                pos_1 = v.vertex;
                if ((v.color.x > 0.0)) {
                    pos_1.xyz = (v.vertex.xyz + (_Scaling.xyz * ((_Scaling.w - 1.0) * v.color.x)));
                };
                float4 tmpvar_16;
                tmpvar_16 = ((_Color * _Multiplier) * tmpvar_15);
                tmpvar_2 = tmpvar_16;
                o.pos = (UnityObjectToClipPos(pos_1));
                o.uv = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
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