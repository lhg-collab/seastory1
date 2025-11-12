Shader "J/OutlineURP"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.15, 0.95, 0.90, 1)
        _OutlineThickness("Thickness (World Units)", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        // ¹Ù±ùÂÊÀ¸·Î »ìÂ¦ ÆØÃ¢ÇØ ¿Ü°û¼±Ã³·³ º¸ÀÌ°Ô ·»´õ
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _OutlineThickness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));
                worldPos += worldNormal * _OutlineThickness;   // ¹ý¼± ¹æÇâÀ¸·Î ÆØÃ¢
                OUT.positionCS = TransformWorldToHClip(worldPos);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
