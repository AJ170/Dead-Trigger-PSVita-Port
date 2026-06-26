// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ProFlares/Textured Flare Shader Gamma with Dirt Layer"
{
	Properties 
   	{
    	_MainTex ( "Texture", 2D )	= "black" {}
		_DirtTex("Dirt Texture", 2D) = "black" {}
  	}
    
	SubShader 
	{
		Tags { "Queue"="Transparent+100" "IgnoreProjector"="True" "RenderType"="Transparent" }

    	Pass 
		{    
			ZWrite Off
      	 	ZTest Always 
      	 	Blend One One
     		
			CGPROGRAM
			
 			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"

			sampler2D	_MainTex;
			sampler2D _DirtTex;
 
           	struct VertInput
            {
                half4 vertex	: POSITION;
                half2 texcoord	: TEXCOORD0;
			  	fixed4 color	: COLOR;
            };

           	struct Verts
            {
                half4 pos		: SV_POSITION;
                half4 uv		: TEXCOORD0;
			  	fixed4 _color   : COLOR;
            };

            Verts vert(VertInput vert)
            {
                Verts v;
                v._color = vert.color * (vert.color.a * 3);
                v.pos = UnityObjectToClipPos(vert.vertex);
                v.uv.xy = vert.texcoord.xy;
                v.uv.zw = ComputeScreenPos(v.pos).xy;
                return v;
            }

            fixed4 frag(Verts v) : COLOR
            {
                half2 screenUV = v.uv.zw / v.pos.w;
                half4 flare = tex2D(_MainTex, v.uv.xy) * v._color;
                half4 lensDirt = tex2D(_DirtTex, screenUV);
                return flare * 0.25 + flare * lensDirt;
            }

			ENDCG
		}
 	}
}


