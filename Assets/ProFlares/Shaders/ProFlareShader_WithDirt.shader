// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "ProFlares/Textured Flare Shader Gamma with Dirt Layer"
{
    Properties
    {
        _MainTex("Texture", 2D) = "black" {}
        _DirtTex("Dirt Texture", 2D) = "black" {}
    }

        SubShader
    {
        Tags { "Queue" = "Transparent+100" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend One One

            CGPROGRAM

            #pragma vertex vertFunc
            #pragma fragment fragFunc
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            sampler2D	_MainTex;
            sampler2D _DirtTex;

            struct VertInput
            {
                half4 vertex	: POSITION;
                half2 texcoord	: TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct VertOutput
            {
                half4 pos		: SV_POSITION;
                half4 uv		: TEXCOORD0;
                fixed4 _color : COLOR;
                half posW       : TEXCOORD1;
            };

            VertOutput vertFunc(VertInput input)
            {
                VertOutput output;
                output._color = input.color * (input.color.a * 3.0h);
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv.xy = input.texcoord.xy;
                output.uv.zw = ComputeScreenPos(output.pos).xy;
                output.posW = output.pos.w;
                return output;
            }

            fixed4 fragFunc(VertOutput input) : COLOR
            {
                half2 screenUV = input.uv.zw / input.posW;
                half4 flare = tex2D(_MainTex, input.uv.xy) * input._color;
                half4 lensDirt = tex2D(_DirtTex, screenUV);
                return flare * 0.25h + flare * lensDirt;
            }
            ENDCG
        }
    }
}