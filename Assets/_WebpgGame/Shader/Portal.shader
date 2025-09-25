Shader "Custom/Portal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "white" {}
        _DistortTex ("Distortion Texture", 2D) = "white" {}
        _Color ("Portal Color", Color) = (0.5, 0.8, 1, 1)
        _Speed ("Rotation Speed", Float) = 1
        _DistortAmount ("Distortion Amount", Float) = 0.1
        _Fresnel ("Fresnel Power", Float) = 2
        _Glow ("Glow Intensity", Float) = 2
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            // Variables del shader
            sampler2D _MainTex;
            sampler2D _DistortTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Speed;
            float _DistortAmount;
            float _Fresnel;
            float _Glow;
            
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
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                
                return o;
            }
            
            // Fragment Shader
            fixed4 frag(vertOutput i) : SV_Target
            {
                // Centro para rotación
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
                float2 distort = tex2D(_DistortTex, rotatedUV + _Time.y * 0.1).rg;
                distort = (distort - 0.5) * _DistortAmount;
                rotatedUV += distort;
                
                // Sample de la textura principal
                fixed4 portal = tex2D(_MainTex, rotatedUV);
                
                // Fresnel para efecto de borde
                float fresnel = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
                fresnel = pow(fresnel, _Fresnel);
                
                // Aplicar color y glow
                portal.rgb *= _Color.rgb * _Glow;
                portal.a *= fresnel;
                
                return portal;
            }
            
            ENDCG
        }
    }
}