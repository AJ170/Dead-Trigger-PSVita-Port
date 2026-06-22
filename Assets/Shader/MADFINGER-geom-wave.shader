Shader "MADFINGER/FX/Geom wave" {
    Properties {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _WaveSpeed ("Waves speed", Vector) = (0.25,0.25,0.25,0.25)
        _WaveSize ("Waves size", Vector) = (0.25,0.25,0.25,0.25)
        _WaveAmplitude ("Waves amp", Vector) = (1,0,0,0)
        _WaveBias ("Waves bias", Vector) = (0,0,0,0)
        _WaveTimeOffs ("Wave time offs", Vector) = (0,0,0,0)
    }
    SubShader { 
        LOD 100
        Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }
        Pass {
            Tags { "LIGHTMODE"="ForwardBase" "RenderType"="Opaque" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _WaveSpeed;
            float4 _WaveSize;
            float4 _WaveAmplitude;
            float4 _WaveBias;
            float4 _WaveTimeOffs;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 tmpvar_1;
                tmpvar_1 = normalize(v.normal);
                float4 tmpvar_2;
                tmpvar_2 = v.vertex;
                float4 tmpvar_3;
                float4 tmpvar_4;
                tmpvar_4 = abs((frac((v.color.zzzz - (-(_WaveSize) + ((_Time.y + _WaveTimeOffs) * _WaveSpeed)))) - _WaveSize));
                float4 tmpvar_5;
                tmpvar_5 = (tmpvar_4 / _WaveSize);
                float4 tmpvar_6;
                //tmpvar_6 = greaterThan (tmpvar_4, _WaveSize);
                //tmpvar_6 = step (tmpvar_4, _WaveSize);
                tmpvar_6 = step (_WaveSize, tmpvar_4);
                //tmpvar_6 = max (tmpvar_4, _WaveSize);
                float4 c_7;
                c_7 = (float4(1.0, 1.0, 1.0, 1.0) - ((tmpvar_5 * tmpvar_5) * (float4(3.0, 3.0, 3.0, 3.0) - (2.0 * tmpvar_5))));
                float tmpvar_8;
                if (tmpvar_6.x) {
                    tmpvar_8 = 0.0;
                } else {
                    tmpvar_8 = c_7.x;
                };
                float tmpvar_9;
                if (tmpvar_6.y) {
                    tmpvar_9 = 0.0;
                } else {
                    tmpvar_9 = c_7.y;
                };
                float tmpvar_10;
                if (tmpvar_6.z) {
                    tmpvar_10 = 0.0;
                } else {
                    tmpvar_10 = c_7.z;
                };
                float tmpvar_11;
                if (tmpvar_6.w) {
                    tmpvar_11 = 0.0;
                } else {
                    tmpvar_11 = c_7.w;
                };
                float4 tmpvar_12;
                tmpvar_12.x = tmpvar_8;
                tmpvar_12.y = tmpvar_9;
                tmpvar_12.z = tmpvar_10;
                tmpvar_12.w = tmpvar_11;
                tmpvar_2.xyz = (v.vertex.xyz + (((tmpvar_1 * dot ((tmpvar_12 + _WaveBias), _WaveAmplitude)) * 1.0) * v.color.w));
                tmpvar_3.xy = v.uv.xy;
                o.pos = (UnityObjectToClipPos(tmpvar_2));
                o.uv = tmpvar_3;
                o.uv1 = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv.xy);
                c_1.w = tmpvar_2.w;
                c_1.xyz = (tmpvar_2.xyz * (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz));
                return c_1;
            }
            ENDCG
        }
    }
}