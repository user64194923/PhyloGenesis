Shader "Custom/SimpleScrollingSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha   // standard alpha blending
            ZWrite Off                        // don’t write depth
            Cull Off                          // show both sides

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Speed;

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

                // scroll UVs
                float2 offset = _Time.y * _Speed.xy;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + offset;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample texture, return as-is (no extra alpha math)
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
