Shader "Custom/SimpleInstancedLeaves"
{
    Properties
    {
        _MainTex ("Leaf Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _WindStrength ("Wind Strength", Range(0, 2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.0
        _AtlasRows ("Atlas Rows", Int) = 2
        _AtlasCols ("Atlas Columns", Int) = 2
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;
            float _WindStrength;
            float _WindSpeed;
            int _AtlasRows;
            int _AtlasCols;

            // Per-instance data
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceData) // xyz = position, w = atlas index
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceRotation) // quaternion
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceScale)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            float3 RotateVector(float3 v, float4 quat)
            {
                return v + 2.0 * cross(quat.xyz, cross(quat.xyz, v) + quat.w * v);
            }

            float2 GetAtlasUV(float2 uv, int atlasIndex)
            {
                int row = atlasIndex / _AtlasCols;
                int col = atlasIndex % _AtlasCols;
                
                float2 atlasUV = uv;
                atlasUV.x = (atlasUV.x + col) / (float)_AtlasCols;
                atlasUV.y = (atlasUV.y + row) / (float)_AtlasRows;
                
                return atlasUV;
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // Get instance data
                float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceData);
                float4 instanceRotation = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceRotation);
                float instanceScale = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceScale);

                // Apply instance transform
                float3 localPos = v.vertex.xyz * instanceScale;
                localPos = RotateVector(localPos, instanceRotation);
                
                // Wind animation
                float windPhase = _Time.y * _WindSpeed + instanceData.x + instanceData.z;
                float windOffset = sin(windPhase) * _WindStrength * 0.1;
                localPos.x += windOffset * v.vertex.y; // More wind at top
                
                float4 worldPos = float4(localPos + instanceData.xyz, 1.0);
                o.vertex = UnityObjectToClipPos(worldPos);
                o.worldPos = worldPos.xyz;

                // Calculate atlas UV
                int atlasIndex = (int)instanceData.w;
                o.uv = GetAtlasUV(v.uv, atlasIndex);

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // Sample texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                fixed4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                col *= instanceColor;

                // Alpha test
                clip(col.a - _Cutoff);

                // Simple lighting based on world position
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                float lighting = dot(float3(0, 0, 1), lightDir) * 0.5 + 0.5;
                col.rgb *= lighting;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Cutout/VertexLit"
}