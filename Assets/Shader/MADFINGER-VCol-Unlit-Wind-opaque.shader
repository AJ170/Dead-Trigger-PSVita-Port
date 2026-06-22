Shader "MADFINGER/Environment/Vertex color + Wind - opaque" {
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Wind ("Wind params", Vector) = (1,1,1,1)
		_WindEdgeFlutter ("Wind edge fultter factor", Float) = 0.5
		_WindEdgeFlutterFreqScale ("Wind edge fultter freq scale", Float) = 0.5
	}
	SubShader { 
		Tags { "RenderType"="Opaque" }
		Pass {
			Tags { "RenderType"="Opaque" }
			Cull Off

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Wind;
            float4 _MainTex_ST;
            float _WindEdgeFlutter;
            float _WindEdgeFlutterFreqScale;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float bendingFact_1;
                float4 wind_2;
                float tmpvar_3;
                tmpvar_3 = v.color.w;
                bendingFact_1 = tmpvar_3;
                float3x3 tmpvar_4;
                tmpvar_4[0] = unity_WorldToObject[0].xyz;
                tmpvar_4[1] = unity_WorldToObject[1].xyz;
                tmpvar_4[2] = unity_WorldToObject[2].xyz;
                wind_2.xyz = mul(tmpvar_4, _Wind.xyz);
                wind_2.w = (_Wind.w * bendingFact_1);
                float2 tmpvar_5;
                tmpvar_5.y = 1.0;
                tmpvar_5.x = _WindEdgeFlutterFreqScale;
                float4 pos_6;
                pos_6.w = v.vertex.w;
                float3 bend_7;
                float4 v_8;
                v_8.x = unity_ObjectToWorld[0].w;
                v_8.y = unity_ObjectToWorld[1].w;
                v_8.z = unity_ObjectToWorld[2].w;
                v_8.w = unity_ObjectToWorld[3].w;
                float tmpvar_9;
                tmpvar_9 = dot (v_8.xyz, float3(1.0, 1.0, 1.0));
                float2 tmpvar_10;
                tmpvar_10.x = dot (v.vertex.xyz, ((_WindEdgeFlutter + tmpvar_9)));
                tmpvar_10.y = tmpvar_9;
                float4 tmpvar_11;
                tmpvar_11 = abs(((frac((((frac((((_Time.y * tmpvar_5).xx + tmpvar_10).xxyy * float4(1.975, 0.793, 0.375, 0.193))) * 2.0) - 1.0) + 0.5)) * 2.0) - 1.0));
                float4 tmpvar_12;
                tmpvar_12 = ((tmpvar_11 * tmpvar_11) * (3.0 - (2.0 * tmpvar_11)));
                float2 tmpvar_13;
                tmpvar_13 = (tmpvar_12.xz + tmpvar_12.yw);
                bend_7.xz = ((_WindEdgeFlutter * 0.1) * normalize(v.normal)).xz;
                bend_7.y = (bendingFact_1 * 0.3);
                pos_6.xyz = (v.vertex.xyz + (((tmpvar_13.xyx * bend_7) + ((wind_2.xyz * tmpvar_13.y) * bendingFact_1)) * wind_2.w));
                pos_6.xyz = (pos_6.xyz + (bendingFact_1 * wind_2.xyz));
                o.pos = (UnityObjectToClipPos(pos_6));
                o.uv = ((v.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                o.color = (v.color * 2.0).xyz;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 tex_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                tex_1.w = tmpvar_2.w;
                tex_1.xyz = (tmpvar_2.xyz * i.color);
                return tex_1;
            }
            ENDCG
        }
    }
}