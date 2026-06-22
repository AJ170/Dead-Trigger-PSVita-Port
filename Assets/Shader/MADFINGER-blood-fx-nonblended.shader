Shader "MADFINGER/FX/Blood FX nonblended" {
    Properties {
        _Params ("Params", Vector) = (0,1,0,0)
        _Color ("Color", Color) = (0,0,0,0)
    }
    SubShader { 
        Tags { "QUEUE"="Overlay" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
        Pass {
            Tags { "QUEUE"="Overlay" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
            ZTest Always
            ZWrite Off
            Cull Off
            Fog {
                Color (0,0,0,0)
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Params;

            float4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float2 tmpvar_2;
                tmpvar_2 = normalize(-(v.vertex.xy));
                float tmpvar_3;
                tmpvar_3 = ((_Time.y * _Params.y) / 1.25);
                float tmpvar_4;
                tmpvar_4 = (frac(abs(tmpvar_3)) * 1.25);
                float tmpvar_5;
                if ((tmpvar_3 >= 0.0)) {
                    tmpvar_5 = tmpvar_4;
                } else {
                    tmpvar_5 = -(tmpvar_4);
                };
                float tmpvar_6;
                if ((tmpvar_5 < 0.5)) {
                    float tmpvar_7;
                    tmpvar_7 = (tmpvar_5 / 0.5);
                    float tmpvar_8;
                    tmpvar_8 = (frac(abs(tmpvar_7)) * 0.5);
                    float tmpvar_9;
                    if ((tmpvar_7 >= 0.0)) {
                      tmpvar_9 = tmpvar_8;
                    } else {
                      tmpvar_9 = -(tmpvar_8);
                    };
                    float tmpvar_10;
                    tmpvar_10 = (20.0 * tmpvar_9);
                    tmpvar_6 = (tmpvar_10 * exp((1.0 - tmpvar_10)));
                } else {
                    tmpvar_6 = 0.0;
                };
                float tmpvar_11;
                if ((_Params.x > 0.0)) {
                  tmpvar_11 = lerp (0.1, 0.0, _Params.x);
                } else {
                  tmpvar_11 = 0.0;
                };
                float4 tmpvar_12;
                tmpvar_12.zw = float2(0.0, 1.0);
                tmpvar_12.xy = (v.vertex.xy * 2.0);
                tmpvar_1.zw = tmpvar_12.zw;
                tmpvar_1.xy = (tmpvar_12.xy - ((tmpvar_2 * (_Params.x + (tmpvar_6 * tmpvar_11))) * v.color.w));
                o.pos = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = _Color;
                return tmpvar_1;
            }
            ENDCG
        }
    }
}