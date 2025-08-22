Shader "Custom/LeafCutoutWindSimple"
{
    Properties
    {
        _MainTex ("Leaf Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.3
        _WindStrength ("Wind Strength", Range(0,2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.0
        _LightTint ("Light Color", Color) = (1,1,1,1)
        _Ambient ("Ambient Strength", Range(0,1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half4 _Color;
            half _Cutoff;
            float _WindStrength;
            float _WindSpeed;
            half4 _LightTint;
            half _Ambient;

            Varyings vert (Attributes v)
            {
                Varyings o;

                // Wind sway
                float3 worldPos = TransformObjectToWorld(v.positionOS).xyz;
                float sway = sin(_Time.y * _WindSpeed + worldPos.x * 0.5 + worldPos.z * 0.5);
                float offset = sway * _WindStrength * (v.positionOS.y + 0.5);
                worldPos.x += offset;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                clip(tex.a - _Cutoff);

                // Simple fake lighting
                float3 lightDir = normalize(float3(0.3, 1.0, 0.2)); // arbitrary sun direction
                float3 normal = float3(0,0,1); // flat normal for quads
                float NdotL = saturate(dot(normal, lightDir));

                half3 litColor = tex.rgb * (_Ambient + NdotL) * _LightTint.rgb;

                return half4(litColor, tex.a);
            }
            ENDHLSL
        }
    }
}
