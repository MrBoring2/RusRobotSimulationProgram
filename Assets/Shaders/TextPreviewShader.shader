Shader "Custom/TextPreviewShader"
{
    Properties
    {
        [PerRendererData]_FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceTex ("Face Texture", 2D) = "white" {}
        [PerRendererData]_OutlineColor ("Outline Color", Color) = (0,0,0,1)
        [PerRendererData]_OutlineWidth ("Outline Width", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZTest Always
        ZWrite Off
        Cull Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "TextMeshPro/DistanceField.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            sampler2D _FaceTex;
            fixed4 _FaceColor;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_FaceTex, i.texcoord) * _FaceColor * i.color;
                return c;
            }
            ENDCG
        }
    }
}
