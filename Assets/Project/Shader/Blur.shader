Shader "Custom/GaussianBlurURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Pass
        {
            // 수평 블러 패스
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 col = fixed4(0,0,0,0);
                float weights[5] = {0.227027f, 0.316216f, 0.070270f, 0.316216f, 0.070270f};
                for (int x = -2; x <= 2; x++)
                {
                    col += tex2D(_MainTex, uv + float2(x,0) * _BlurSize * _MainTex_TexelSize.xy) * weights[abs(x)];
                }
                return col;
            }
            ENDCG
        }
        Pass
        {
            // 수직 블러 패스 (위와 유사하지만 float2(x,0) 대신 float2(0,y) 적용)
            // ...
        }
    }
}
