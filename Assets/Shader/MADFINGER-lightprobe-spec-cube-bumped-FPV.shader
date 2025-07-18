Shader "MADFINGER/Environment/Bumped cubemap specular + Lightprobe FPV"
{
    Properties
    {
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _NormalsTex ("Normalmap", 2D) = "bump" {}
        _SpecCubeTex ("SpecCube", CUBE) = "black" {}
        _SpecularStrength ("Specular strength", Float) = 1
        _SHLightingScale ("LightProbe influence scale", Float) = 1
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

            float _SHLightingScale;

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float2 uv4 : TEXCOORD4;
                float3 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float3 normal = normalize(v.normal);
                float4 tangent = v.tangent;
                float3 tangentDir = normalize(tangent.xyz);
                float3x3 worldMatrix = float3x3(
                    unity_ObjectToWorld[0].xyz,
                    unity_ObjectToWorld[1].xyz,
                    unity_ObjectToWorld[2].xyz
                );

                float3 worldNormal = normalize(mul(worldMatrix, normal));
                float3 worldTangent = normalize(mul(worldMatrix, tangentDir));
                float3 worldBinormal = normalize(cross(worldNormal, worldTangent) * tangent.w);

                float3 T = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
                float3 B = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
                float3 N = float3(worldTangent.z, worldBinormal.z, worldNormal.z);

                // Spherical harmonics lighting (LightProbe)
                float4 normal4 = float4(worldNormal, 1.0);
                float3 shDiffuse =
                      float3(dot(unity_SHAr, normal4), dot(unity_SHAg, normal4), dot(unity_SHAb, normal4))
                    + float3(dot(unity_SHBr, normal4.xyzz * normal4.yzzx),
                             dot(unity_SHBg, normal4.xyzz * normal4.yzzx),
                             dot(unity_SHBb, normal4.xyzz * normal4.yzzx))
                    + unity_SHC.xyz * (normal4.x * normal4.x - normal4.y * normal4.y);

                // Set outputs
                o.pos = UnityObjectToClipPos(v.vertex); // ✅ Fixed projection for depth sorting
                o.uv = v.uv;
                o.uv1 = mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos;
                o.uv2 = T.xy;
                o.uv3 = B.xy;
                o.uv4 = N.xy;
                o.color = shDiffuse * _SHLightingScale * 100.0; // ✅ Boost lighting

                return o;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                float4 tex = tex2D(_MainTex, i.uv);
                return float4(tex.rgb * i.color, tex.a);
            }
            ENDCG
        }
    }
}
