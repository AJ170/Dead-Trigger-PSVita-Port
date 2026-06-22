Shader "MADFINGER/Utils/Blinder" {
	SubShader { 
		LOD 100
		Tags { "QUEUE"="Geometry+1000" "RenderType"="Opaque" }
		Pass {
			Tags { "QUEUE"="Geometry+1000" "RenderType"="Opaque" }

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                return o;
            }
            half4 frag(v2f i) : COLOR
            {
                return float4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
    }
}