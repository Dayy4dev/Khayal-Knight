Shader "Custom/AdditiveVFX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha One
        Cull Off Lighting Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;
            float _Intensity;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);
                // Multiply RGB by intensity and tint
                col.rgb *= _TintColor.rgb * _Intensity * i.color.rgb;
                // Calculate alpha from luminance (r+g+b)/3 or just use the color's brightness
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.a = lum * _TintColor.a * i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
