Shader "MADFINGER/Environment/Blinking emissive" {
    Properties {
        _MainTex ("Base texture", 2D) = "white" {}
        _IntensityScaleBias ("Intensity scale X / bias Y", Vector) = (1,0.1,0,0)
        _SwitchOnOffDuration ("Switch ON (X) / OFF (Y) duration", Vector) = (1,3,0,0)
        _BlinkingRate ("Blinking rate", Float) = 10
        _RndGridSize ("Randomization grid size", Float) = 5
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

            float2 _IntensityScaleBias;
            float2 _SwitchOnOffDuration;
            float _BlinkingRate;

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

                float seed_1;
                float4 tmpvar_2;
                float tmpvar_3;
                tmpvar_3 = (dot (v.color, v.color) * 40.0);
                seed_1 = tmpvar_3;
                float time_4;
                time_4 = (_Time.y * _BlinkingRate);
                float tmpvar_5;
                tmpvar_5 = float((abs(cos(((17.0 * sin((time_4 * 5.0))) + (10.0 * sin(((seed_1 + (time_4 * 3.0)) + 7.993)))))) > 0.5));
                float2 tmpvar_6;
                tmpvar_6 = (_SwitchOnOffDuration * (0.8 + (0.4 * frac(seed_1))));
                float y_7;
                y_7 = (tmpvar_6.x + tmpvar_6.y);
                float tmpvar_8;
                tmpvar_8 = ((_Time.y + seed_1) / y_7);
                float tmpvar_9;
                tmpvar_9 = (frac(abs(tmpvar_8)) * y_7);
                float tmpvar_10;
                if ((tmpvar_8 >= 0.0)) {
                    tmpvar_10 = tmpvar_9;
                } else {
                    tmpvar_10 = -(tmpvar_9);
                };
                float4 tmpvar_11;
                tmpvar_11 = (((v.color * (tmpvar_5 * float((tmpvar_10 < tmpvar_6.x)))) * _IntensityScaleBias.x) + _IntensityScaleBias.y);
                tmpvar_2 = tmpvar_11;
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