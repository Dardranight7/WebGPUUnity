Shader "Custom/Portal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "white" {}
        _DistortTex ("Distortion Texture", 2D) = "white" {}
        [HDR] _Color ("Portal Color", Color) = (0.5, 0.8, 1, 1)
        _Speed ("Rotation Speed", Float) = 1
        _DistortAmount ("Distortion Amount", Float) = 0.1
        _Fresnel ("Fresnel Power", Float) = 2
        _Glow ("Glow Intensity", Float) = 2
        _Metallic ("Metallic", Range(0,1))= 0.8
        _Smoothness ("Smoothness", Range(0,1))= 0.8
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Variables del shader
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _Speed;
            float _DistortAmount;
            float _Fresnel;
            float _Glow;
            float  _Metallic;
            float _Smoothness;
            CBUFFER_END     
            
            // Estructura de entrada
            struct vertInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            // Estructura de salida del vertex hacia fragment
            struct vertOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };
            
            // Vertex Shader
            vertOutput vert(vertInput v)
            {
                vertOutput o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.viewDir = normalize(GetWorldSpaceViewDir(worldPos));;
                
                return o;
            }
            
            // Fragment Shader
            half4 frag(vertOutput i) : SV_Target
            {
                // Centro para rotaciónworldPos 
                float2 center = float2(0.5, 0.5);
                float2 uv = i.uv - center;
                
                // Rotación
                float angle = _Time.y * _Speed;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotatedUV = float2(
                    cosA * uv.x - sinA * uv.y,
                    sinA * uv.x + cosA * uv.y
                ) + center;
                
                // Distorsión con noise
                float2 distort = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, rotatedUV + _Time.y * 0.1).rg;;
                distort = (distort - 0.5) * _DistortAmount;
                rotatedUV += distort;
                
                // Sample de la textura principal
                half4 portal = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,rotatedUV);
                
                // Fresnel para efecto de borde
                float fresnel = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
                fresnel = pow(fresnel, _Fresnel);

                // Efecto metálico - brillo especular
                float3 reflectDir = reflect(-i.viewDir, i.worldNormal);
                float spec = pow(saturate(dot(reflectDir, i.viewDir)), _Smoothness * 128);
                float3 metallic = spec * _Metallic * fresnel;

                //fragment
                float pulse = 1.0 + sin(_Time.y*2)* 0.3;
                portal.rgb *= pulse;

                // Aplicar color, glow y metálico
                portal.rgb *= _Color.rgb * _Glow;
                portal.rgb += metallic;
                //portal.a *= fresnel * 2 + 0.2;
                portal.a *= fresnel + 0.6;
                
                return portal;
            }
            ENDHLSL
            

        }
    }
}