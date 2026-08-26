Shader "Vita/Transparent/Blinking God Rays with Slope Fade" {
    Properties {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        
        _Multiplier ("Brightness Multiplier", Float) = 1.0
        _Bias ("Brightness Bias", Float) = 0.0
        
        _FadeOutNear ("Near Fade Distance", Float) = 10.0
        _FadeOutFar ("Far Fade Distance", Float) = 10000.0
        
        _OnDuration ("Blink ON Duration (seconds)", Float) = 0.5
        _OffDuration ("Blink OFF Duration (seconds)", Float) = 0.5
        _TimeOffset ("Time Offset Scale", Float) = 5.0
        
        _NoiseAmount ("Noise Amount (0 = smooth pulse)", Range(0, 0.5)) = 0.0
    }

    SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "true" }
        LOD 100
        
        Blend One One
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Color(0, 0, 0, 0) }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            half4 _Color;
            half _Multiplier;
            half _Bias;
            
            half _FadeOutNear;
            half _FadeOutFar;
            
            half _OnDuration;
            half _OffDuration;
            half _TimeOffset;
            
            half _NoiseAmount;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            v2f vert(appdata v) {
                v2f o;

                // === TIME CALCULATION ===
                half time = _Time.y + _TimeOffset * v.color.z;

                // === DISTANCE-BASED FADING ===

                // Calculate camera distance
                float3 viewPos = mul(UNITY_MATRIX_MV, v.vertex).xyz;
                half dist = length(viewPos);

                // Near fade: opacity increases from 0 at camera to 1 at _FadeOutNear
                //half nearFade = saturate(dist / _FadeOutNear);
                //nearFade = nearFade * nearFade; // Smoother falloff with squared curve

                // Far fade: opacity decreases after _FadeOutFar
                //half farFade = saturate(1.0 - (max(dist - _FadeOutFar, 0.0) * 0.2));
                half farFade = saturate(dist/_FadeOutFar);
                farFade = farFade * farFade;

                //half distanceFade = nearFade * farFade;
                half distanceFade = max(0.25f, farFade);

                // === SLOPE-BASED FADEOUT ===
                // Fades out geometry viewed edge-on (good for god rays)

                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // Dot product of normal and view direction (0 = edge-on, 1 = facing camera)
                half slopeFade = abs(dot(worldNormal, viewDir));

                // === BLINKING/PULSING ANIMATION ===

                half cycleDuration = _OnDuration + _OffDuration;
                half cycleTime = fmod(time, cycleDuration);
                
                // Smooth pulse wave
                half t1 = saturate(cycleTime / (_OnDuration * 0.25));
                half t2 = saturate((cycleTime - _OnDuration * 0.75) / (_OnDuration * 0.25));
                
                half pulse = t1 * (1.0 - t2);
                pulse = pulse * pulse * (3.0 - 2.0 * pulse); // Smoothstep curve

                // Optional noise modulation
                half wave = pulse;
                if (_NoiseAmount > 0.01)
                {
                    // Cheap noise approximation
                    half angle = time * (6.283 / _OnDuration);
                    half noise = sin(angle) * 0.5;
                    wave = lerp(pulse, noise, _NoiseAmount);
                }

                wave += _Bias;

                // === FINAL OUTPUT ===

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Combine all factors
                o.color = distanceFade * slopeFade * wave * _Color * _Multiplier;

                return o;
            }

            half4 frag(v2f i) : SV_Target {
                half4 texColor = tex2D(_MainTex, i.uv);
                return texColor * i.color;
            }
            ENDCG
        }
    }

    Fallback "Transparent/VertexLit"
}