Shader "MADFINGER/Environment/Cube env map (Supports LightProbes) FPV"
{
    Properties
    {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _EnvTex ("Cube env tex", CUBE) = "black" {}
        _SHLightingScale ("LightProbe influence scale", Float) = 1
        _EnvStrength ("Env strength weights", Vector) = (0,0,0,2)
        _Params ("x - FPV proj", Vector) = (1,0,0,0)
        _UVScrollSpeed ("UV scroll speed XY", Vector) = (0,0,0,0)
    }

    SubShader
    {
        LOD 100
        Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _MainTex_ST;
            float _SHLightingScale;
            float4 _ProjParams;
            float4 _Params;
            float4 _UVScrollSpeed;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 normal = normalize(v.normal);
                float4 clipPos = UnityObjectToClipPos(v.vertex); // ✅ Fixed depth issue

                float3x3 worldNormalMat = float3x3(
                    unity_ObjectToWorld[0].xyz,
                    unity_ObjectToWorld[1].xyz,
                    unity_ObjectToWorld[2].xyz
                );
                float3 worldNormal = mul(worldNormalMat, normal);

                float3 viewDir = mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos;
                float3 reflVec = viewDir - 2.0 * dot(worldNormal, viewDir) * worldNormal;
                float3 cubeUV = float3(-reflVec.x, reflVec.y, reflVec.z); // used in original, not sampled

                float4 normal4 = float4(worldNormal, 1.0);
                float3 shDiffuse = 
                      float3(dot(unity_SHAr, normal4), dot(unity_SHAg, normal4), dot(unity_SHAb, normal4))
                    + float3(dot(unity_SHBr, normal4.xyzz * normal4.yzzx),
                             dot(unity_SHBg, normal4.xyzz * normal4.yzzx),
                             dot(unity_SHBb, normal4.xyzz * normal4.yzzx))
                    + unity_SHC.xyz * (normal4.x * normal4.x - normal4.y * normal4.y);

                o.pos = clipPos;
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw + frac(_UVScrollSpeed.xy * _Time.xy);
                o.uv1 = cubeUV.xy;
                o.color = shDiffuse * _SHLightingScale * 100.0; // ✅ Bright lighting

                return o;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                float4 texColor = tex2D(_MainTex, i.uv);
                return float4(texColor.rgb * i.color, texColor.a);
            }
            ENDCG
        }
    }
}
