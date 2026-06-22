// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "MADFINGER/Characters/Character shadow plane - sphere AO" {
	Properties {
		_Sphere0 ("S0", Vector) = (0,0,0,0)
		_Sphere1 ("S0", Vector) = (0,0,0,0)
		_Sphere2 ("S0", Vector) = (0,0,0,0)
		_Intensity ("Intensity", Float) = 0.9
	}
	SubShader { 
		Tags { "QUEUE"="Transparent-15" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
		Pass {
			Tags { "QUEUE"="Transparent-15" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
			ZWrite Off
			Cull Off
			Fog {
				Color (0,0,0,0)
			}
			Blend DstColor Zero
			ColorMask RGB

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Sphere0;
            float4 _Sphere1;
            float4 _Sphere2;
            float _Intensity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float4 tmpvar_1;
                float3 tmpvar_2;
                tmpvar_2 = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3x3 tmpvar_3;
                tmpvar_3[0] = unity_ObjectToWorld[0].xyz;
                tmpvar_3[1] = unity_ObjectToWorld[1].xyz;
                tmpvar_3[2] = unity_ObjectToWorld[2].xyz;
                float3 tmpvar_4;
                tmpvar_4 = mul(tmpvar_3, normalize(v.normal));
                float3 tmpvar_5;
                tmpvar_5 = (_Sphere0.xyz - tmpvar_2);
                float tmpvar_6;
                tmpvar_6 = sqrt(dot (tmpvar_5, tmpvar_5));
                float tmpvar_7;
                tmpvar_7 = (_Sphere0.w / tmpvar_6);
                float3 tmpvar_8;
                tmpvar_8 = (_Sphere1.xyz - tmpvar_2);
                float tmpvar_9;
                tmpvar_9 = sqrt(dot (tmpvar_8, tmpvar_8));
                float tmpvar_10;
                tmpvar_10 = (_Sphere1.w / tmpvar_9);
                float3 tmpvar_11;
                tmpvar_11 = (_Sphere2.xyz - tmpvar_2);
                float tmpvar_12;
                tmpvar_12 = sqrt(dot (tmpvar_11, tmpvar_11));
                float tmpvar_13;
                tmpvar_13 = (_Sphere2.w / tmpvar_12);
                float tmpvar_14;
                tmpvar_14 = (max ((1.0 - clamp (((((dot (tmpvar_4, (tmpvar_5 / tmpvar_6)) * tmpvar_7) * tmpvar_7) + ((dot (tmpvar_4, (tmpvar_8 / tmpvar_9)) * tmpvar_10) * tmpvar_10)) + ((dot (tmpvar_4, (tmpvar_11 / tmpvar_12)) * tmpvar_13) * tmpvar_13)), 0.0, 1.0)), (1.0 - _Intensity)) + (1.0 - v.color.x));
                float4 tmpvar_15;
                tmpvar_15.x = tmpvar_14;
                tmpvar_15.y = tmpvar_14;
                tmpvar_15.z = tmpvar_14;
                tmpvar_15.w = tmpvar_14;
                tmpvar_1 = tmpvar_15;
                o.pos = (UnityObjectToClipPos(v.vertex));
                o.color = tmpvar_1;

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                return i.color;
            }
            ENDCG
        }
    }
}