Shader "Custom/GyzmoManipulatorShader"
{
   Properties
    {
        _Color ("Color", Color) = (1,1,1,0.5)  // Альфа = 0.5 по умолчанию
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 1)) = 1.0
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
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float _AlphaMultiplier;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= _AlphaMultiplier;  // Контроль прозрачности
                return col;
            }
            ENDCG
        }
    }
}