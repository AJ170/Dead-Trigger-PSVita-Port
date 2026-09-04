Shader "UI/ColorGradingOverlay" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _ColorBias("Color Bias", Color) = (0.03, -0.05, -0.1, 0)
        //_BiasIntensity("Bias Intensity", Range(0, 2)) = 1.0
    }
    
    SubShader {
        Tags { "Queue" = "Overlay" "CanUseSpriteAtlas" = "false" }
        LOD 100
        
        Pass {
            //Blend One One  // Additive blend for positive values
            //Blend DstAlpha One	//This doesn't seem too bad for the tint blending
            Blend DstColor Zero
            ZTest Always
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            fixed4 _ColorBias;
            //float _BiasIntensity;
            
            v2f vert(appdata IN) {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target {
                
                return fixed4(1.0f, 0.93f, 0.87f, 1.0f);
            }
            ENDCG
        }
    }
}

