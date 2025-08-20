Shader "Custom/SimpleLeafCutoutWind"
{
    Properties
    {
        _MainTex ("Leaf Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.3
        _WindStrength ("Wind Strength", Range(0,2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed  _Cutoff;
            float  _WindStrength;
            float  _WindSpeed;

            v2f vert (appdata v)
            {
                v2f o;

                // Wind sway based on world position + time
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float sway = sin(_Time.y * _WindSpeed + worldPos.x * 0.5 + worldPos.z * 0.5);
                
                // Apply more wind to top of the quad (v.vertex.y in local space)
                float offset = sway * _WindStrength * (v.vertex.y + 0.5);
                worldPos.x += offset;

                o.pos = UnityWorldToClipPos(float4(worldPos, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                clip(col.a - _Cutoff);
                return col;
            }
            ENDHLSL
        }
    }
}
