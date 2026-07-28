Shader "MADFINGER/PostFX/ExplosionFX" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "" {}
        _UVOffsAndAspectScale("UV Offset (xy) + Aspect Scale (zw)", Vector) = (0,0,0,0)

        _Wave0ParamSet0("Wave0 Center (xy), Distortion Amp (z), Distortion Speed (w)", Vector) = (0,0,0,0)
        _Wave0ParamSet1("Wave0 Inv Ring Width (x), Wave Speed (y), Wavefront Pos (z)", Vector) = (0,0,0,0)

        _Wave1ParamSet0("Wave1 Center (xy), Distortion Amp (z), Distortion Speed (w)", Vector) = (0,0,0,0)
        _Wave1ParamSet1("Wave1 Inv Ring Width (x), Wave Speed (y), Wavefront Pos (z)", Vector) = (0,0,0,0)

        _Wave2ParamSet0("Wave2 Center (xy), Distortion Amp (z), Distortion Speed (w)", Vector) = (0,0,0,0)
        _Wave2ParamSet1("Wave2 Inv Ring Width (x), Wave Speed (y), Wavefront Pos (z)", Vector) = (0,0,0,0)

            // NOTE: the decompiled source declared _Wave3ParamSet0/1 in Properties but never
            // referenced them in either the vertex or fragment shader. This looks like a leftover
            // from a 4-wave version of the effect that was later cut down to 3 waves. They're
            // removed here since they do nothing; see the accompanying write-up before deleting
            // them from a live project in case the C# controller still calls SetVector() on them
            // (harmless if so, Unity just no-ops/warns).

            _Color0("Color0 (hot/core color)", Color) = (1,1,1,0)
            _Color1("Color1 (cool/edge color)", Color) = (0.5,0.3,0,1)
            _Params("Params (x = global intensity)", Vector) = (0,0,0,0)
    }
        SubShader{
            Pass {
                ZTest Always
                ZWrite Off
                Cull Off
                Fog { Mode Off }

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                float4 _UVOffsAndAspectScale;
                float4 _Wave0ParamSet0;
                float4 _Wave0ParamSet1;
                float4 _Wave1ParamSet0;
                float4 _Wave1ParamSet1;
                float4 _Wave2ParamSet0;
                float4 _Wave2ParamSet1;
                float4 _Color0;
                float4 _Color1;
                float4 _Params;

                sampler2D _MainTex;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos   : SV_POSITION;
                    float2 uv    : TEXCOORD0;
                    float4 color : COLOR;
                };

                struct WaveResult
                {
                    float2 uvOffset; // radial UV distortion contributed by this wave
                    float4 color;    // additive glow color contributed by this wave
                };

                // Computes one explosion shockwave's contribution to UV distortion and glow color.
                // vertexPos    - incoming quad vertex position (0..1 space)
                // paramSet0.xy - wave center in the same 0..1 space
                // paramSet0.z  - distortion amplitude
                // paramSet0.w  - distortion oscillation speed
                // paramSet1.x  - inverse ring width (bigger = thinner ring)
                // paramSet1.y  - outward wave travel speed
                // paramSet1.z  - wavefront position (how far the shockwave has traveled, in ring-widths)
                WaveResult ComputeWave(float2 vertexPos, float4 paramSet0, float4 paramSet1, float2 aspectScale, float globalIntensity)
                {
                    WaveResult result;

                    // Vector from the explosion center to this vertex, aspect-corrected so the
                    // ring reads as circular rather than stretched to the screen's aspect ratio.
                    float2 delta = (vertexPos - paramSet0.xy) * aspectScale;
                    float dist = sqrt(dot(delta, delta));

                    // --- Radial UV distortion (the "heat ripple" push/pull on the background) ---

                    // Grows from 0 as the wavefront reaches this vertex's radius, then keeps
                    // climbing behind the front (scaled into "oscillation time" by paramSet0.w).
                    float ripplePhase = max(paramSet1.z - dist / paramSet1.y, 0.0) * paramSet0.w;

                    // A damped sine: sin(phase) * 1/(1+phase^2). Rings out from the impact point
                    // and decays with distance-behind-the-front rather than with time directly.
                    float rippleStrength = (sin(ripplePhase) * (1.0 / (1.0 + ripplePhase * ripplePhase))) * paramSet0.z;

                    // Fades the distortion to zero right at the wave center, so UVs don't fold
                    // in on themselves at dist == 0.
                    float centerFade = clamp(dist * paramSet1.x, 0.0, 1.0);

                    result.uvOffset = (rippleStrength * (delta / dist)) * (1.0 - centerFade * centerFade);

                    // --- Glow ring color ---

                    // Where the wavefront currently sits, remapped into a 0..~16 "shape" range.
                    float wavefrontPos = max(paramSet1.z, 0.0) * 16.25;

                    // Peaked falloff curve (x * e^(1-x)): rises from 0, peaks at x==1, decays after.
                    // The + 0.0001 keeps it away from an exact 0 so later divides are safe.
                    float wavefrontShape = (wavefrontPos * exp(1.0 - wavefrontPos)) + 0.0001;

                    float ringWidth = 1.0 / paramSet1.x;
                    // Artist-tuned kink: rings past a certain width get doubled, presumably so
                    // large/slow explosions don't thin out into an invisible line.
                    if (ringWidth > 0.65)
                    {
                        ringWidth *= 2.0;
                    }

                    // 1 at the ring center, falling off to 0 at its inner/outer edge.
                    float ringMask = 1.0 - clamp(dist / (ringWidth * wavefrontShape), 0.0, 1.0);

                    result.color = ((((ringMask * ringMask) * paramSet1.x) * lerp(_Color1, _Color0, wavefrontShape.xxxx)) * 1.5 * wavefrontShape) * globalIntensity;

                    return result;
                }

                v2f vert(appdata_t v)
                {
                    v2f o;

                    float2 aspectScale = _UVOffsAndAspectScale.zw;

                    WaveResult wave0 = ComputeWave(v.vertex.xy, _Wave0ParamSet0, _Wave0ParamSet1, aspectScale, _Params.x);
                    WaveResult wave1 = ComputeWave(v.vertex.xy, _Wave1ParamSet0, _Wave1ParamSet1, aspectScale, _Params.x);
                    WaveResult wave2 = ComputeWave(v.vertex.xy, _Wave2ParamSet0, _Wave2ParamSet1, aspectScale, _Params.x);

                    // This is a fullscreen quad: incoming vertex.xy is in 0..1 space, mapped
                    // straight to clip space (-1..1).
                    float4 clipPos;
                    clipPos.xy = (v.vertex.xy * 2.0) - float2(1.0, 1.0);
                    clipPos.zw = float2(0.0, 1.0);

                    // Flip clip-space Y when _UVOffsAndAspectScale.y is negative. This is a
                    // manual stand-in for what Unity's UNITY_UV_STARTS_AT_TOP / _ProjectionParams.x
                    // machinery normally does for you -- see the accompanying write-up regarding
                    // whether this will behave the same on PS Vita as it does in-editor.
                    float flipY = -1.0;// (_UVOffsAndAspectScale.y < 0.0) ? -1.0 : 1.0;
                    clipPos.y *= flipY;

                    o.pos = clipPos;
                    o.uv = v.vertex.xy + _UVOffsAndAspectScale.xy + wave0.uvOffset + wave1.uvOffset + wave2.uvOffset;
                    o.color = wave0.color + wave1.color + wave2.color;

                    return o;
                }

                half4 frag(v2f i) : COLOR
                {
                    return tex2D(_MainTex, i.uv) + i.color;
                }
                ENDCG
            }
        }
}
