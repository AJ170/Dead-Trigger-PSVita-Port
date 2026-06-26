Shader "Custom/RTBlendShader"
{
    Properties
    {
        // _MainTex is the default input from Graphics.Blit(rtA, ...)
        _MainTex("Texture A (Base)", 2D) = "white" {}
        _SecondTex("Texture B (Target)", 2D) = "white" {}
        _BlendValue("Blend (0 to 1)", Range(0.0, 1.0)) = 0.5
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                sampler2D _SecondTex;
                uniform float _BlendValue;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Sample both RenderTextures
                    fixed4 colA = tex2D(_MainTex, i.uv);
                    fixed4 colB = tex2D(_SecondTex, i.uv);

                    // Linearly interpolate between colors based on _BlendValue
                    return lerp(colA, colB, _BlendValue);
                }
                ENDCG
            }
        }
}