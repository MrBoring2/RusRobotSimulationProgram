Shader "Custom/GyzmoManipulatorShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.5)
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 1)) = 1.0
        
        // Параметры для объёма/освещения
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _LightingStrength ("Lighting Strength", Range(0,1)) = 1.0
    }
    
    SubShader
    {
        Tags { 
            "Queue" = "Transparent+1000"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // Для освещения
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; // Добавляем нормали
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _AlphaMultiplier;
            float _Glossiness;
            float _Metallic;
            float _LightingStrength;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // Преобразуем нормаль в мировое пространство
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                // Направление взгляда
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Базовая текстура и цвет
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Нормализуем нормаль
                float3 normal = normalize(i.worldNormal);
                
                // Основное направленное освещение
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                
                // Диффузное освещение (Ламберт)
                float NdotL = max(0.0, dot(normal, lightDir));
                float3 diffuse = lightColor * NdotL;
                
                // Ambient (фоновое освещение)
                float3 ambient = ShadeSH9(half4(normal, 1));
                
                // Specular/Glossy (блики)
                float3 halfVec = normalize(lightDir + i.viewDir);
                float NdotH = max(0.0, dot(normal, halfVec));
                float spec = pow(NdotH, exp2(10.0 * _Glossiness + 1.0));
                float3 specular = lightColor * spec * _Metallic;
                
                // Комбинируем освещение
                float3 lighting = ambient + (diffuse + specular) * _LightingStrength;
                
                // Применяем освещение к цвету
                col.rgb *= lighting;
                
                // Контроль прозрачности
                col.a *= _AlphaMultiplier;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}