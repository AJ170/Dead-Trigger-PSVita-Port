Shader "MADFINGER/FX/Blood FX blended" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Params ("Params", Vector) = (0,1,0,0)
        _ColorBooster ("Color booster", Color) = (0.5,0,0,0)
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
            Blend DstColor SrcColor

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Params;
            float4 _ColorBooster;

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
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float4 tmpvar_2;
                float2 tmpvar_3;
                tmpvar_3 = normalize(-(v.vertex.xy));
                float tmpvar_4;
                tmpvar_4 = ((_Time.y * _Params.y) / 1.25);
                float tmpvar_5;
                tmpvar_5 = (frac(abs(tmpvar_4)) * 1.25);
                float tmpvar_6;
                if ((tmpvar_4 >= 0.0)) {
                    tmpvar_6 = tmpvar_5;
                } else {
                    tmpvar_6 = -(tmpvar_5);
                };
                float tmpvar_7;
                if ((tmpvar_6 < 0.5)) {
                    float tmpvar_8;
                    tmpvar_8 = (tmpvar_6 / 0.5);
                    float tmpvar_9;
                    tmpvar_9 = (frac(abs(tmpvar_8)) * 0.5);
                    float tmpvar_10;
                    if ((tmpvar_8 >= 0.0)) {
                        tmpvar_10 = tmpvar_9;
                    } else {
                        tmpvar_10 = -(tmpvar_9);
                    };
                    float tmpvar_11;
                    tmpvar_11 = (20.0 * tmpvar_10);
                    tmpvar_7 = (tmpvar_11 * exp((1.0 - tmpvar_11)));
                } else {
                    tmpvar_7 = 0.0;
                };
                float tmpvar_12;
                if ((_Params.x > 0.0)) {
                    tmpvar_12 = lerp (0.1, 0.0, _Params.x);
                } else {
                    tmpvar_12 = 0.0;
                };
                float tmpvar_13;
                if ((_Params.x > 0.0)) {
                    tmpvar_13 = lerp (1.0, 0.0, _Params.x);
                } else {
                    tmpvar_13 = 0.0;
                };
                float4 tmpvar_14;
                tmpvar_14.zw = float2(0.0, 1.0);
                tmpvar_14.xy = (v.vertex.xy * 2.0);
                tmpvar_1.zw = tmpvar_14.zw;
                float4 tmpvar_15;
                tmpvar_15.xyz = (((_ColorBooster.xyz * tmpvar_13) * v.color.w) * tmpvar_7);
                tmpvar_15.w = v.color.w;
                tmpvar_2 = tmpvar_15;
                tmpvar_1.xy = (tmpvar_14.xy - ((tmpvar_3 * (_Params.x + (tmpvar_7 * tmpvar_12))) * v.color.w));
                o.pos = tmpvar_1;
                o.uv = v.uv.xy;
                o.color = tmpvar_2;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tmpvar_1;
                tmpvar_1 = tex2D (_MainTex, i.uv);
                return tmpvar_1;
            }
            ENDCG
        }
    }
}