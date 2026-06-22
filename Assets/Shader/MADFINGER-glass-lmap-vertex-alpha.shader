// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Glass/Lightmap + cube env + per vertex alpha" {
	Properties {
		_MainTex ("Diffuse", 2D) = "white" {}
		_EnvMap ("EnvMap", CUBE) = "black" {}
		_ReflectivityMaskWeights ("Reflectivity Mask Weights (RGB)", Vector) = (0.3,0.59,0.11,0)
	}
	SubShader { 
		LOD 100
		Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            samplerCUBE _EnvMap;
            float4 _ReflectivityMaskWeights;

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
                float3 uv2 : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3x3 tmpvar_1;
                tmpvar_1[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_1[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_1[2] = unity_ObjectToWorld[2].xyz;
                float3 tmpvar_2;
                tmpvar_2 = normalize(mul(tmpvar_1, normalize(v.normal)));
                float3 i_3;
                i_3 = (mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos);
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.uv = v.uv.xy;
                o.uv1 = ((v.uv1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
                o.uv2 = (i_3 - (2.0 * (dot (tmpvar_2, i_3) * tmpvar_2)));
                o.color = v.color;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                float4 c_1;
                float4 tmpvar_2;
                tmpvar_2 = tex2D (_MainTex, i.uv);
                c_1.w = tmpvar_2.w;
                float4 tmpvar_3;
                tmpvar_3 = texCUBE (_EnvMap, i.uv2);
                float3 tmpvar_4;
                tmpvar_4 = (tmpvar_2.xyz + (tmpvar_3 * dot (tmpvar_2.xyz, _ReflectivityMaskWeights.xyz)).xyz);
                c_1.xyz = tmpvar_4;
                c_1.xyz = (c_1.xyz * (2.0 * UNITY_SAMPLE_TEX2D (unity_Lightmap, i.uv1).xyz));
                c_1.w = i.color.w;
                return c_1;
            }
            ENDCG
        }
    }
}