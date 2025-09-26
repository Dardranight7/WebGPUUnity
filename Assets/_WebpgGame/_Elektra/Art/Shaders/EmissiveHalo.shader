Shader "Custom/na"
{
    Properties
    {
          [HDR] _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
          _GlowIntensity ("Glow Intensity", Float) = 2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha One
        ZWrite Off


        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _GlowIntensity;
            CBUFFER_END
            
            struct vertInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

            };

            struct vertOutput
            {
                float4 pos : SV_POSITION;
                                  float3 worldNormal : TEXCOORD0;
                  float3 viewDir : TEXCOORD1;
            };

            vertOutput vert(vertInput v)
            {
                vertOutput o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);

                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                  o.worldNormal = TransformObjectToWorldNormal(v.normal);
                  o.viewDir = normalize(GetWorldSpaceViewDir(worldPos));

                
                return o;
            }

            half4 frag(vertOutput i) : SV_Target
            {
                  float fresnel = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
                  half4 glow = _GlowColor * _GlowIntensity;
                  glow.a = fresnel * 0.8; // Transparente en centro, brillante en bordes
                  return glow;

            }
            ENDHLSL
        }
    }
}
