Shader "Custom/EmissiveGlow"
{
    Properties
    {
          [HDR] _EmissionColor ("Emission Color", Color) = (0, 1, 1, 1)
          _EmissionStrength ("Emission Strength", Float) = 2

    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="TransparentCutout" "RenderPipeline" = "UniversalPipeline"}


        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _EmissionColor;
                float _EmissionStrength;

            CBUFFER_END

            struct vertInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct vertOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            vertOutput vert(vertInput v)
            {
                vertOutput o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(vertOutput i) : SV_Target
            {
              half4 emission = _EmissionColor * _EmissionStrength;
              emission.rgb +=  emission.rgb + pow(emission.rgb, 0.5) *2;
              return emission;
            }
            ENDHLSL
        }
    }
}
