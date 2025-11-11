Shader "URP/Outline/InvertedHull"
{
    Properties{
        _OutlineColor("Outline Color", Color) = (1,0.7,0.2,1)
        _OutlineWidth("Outline Width (m)", Float) = 0.02
    }
    SubShader{
        Tags{ "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass{
            Name "Outline"
            Cull Front          // µÚÁýÈù ¿Ü°û¸¸ ±×¸²
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes{
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings{
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            Varyings vert (Attributes v){
                Varyings o;
                float3 nWS  = TransformObjectToWorldNormal(v.normalOS);
                float3 pWS  = TransformObjectToWorld(v.positionOS.xyz);
                pWS += nWS * _OutlineWidth; // ¿ùµå ´ÜÀ§·Î ¹Ù±ùÀ¸·Î ÆØÃ¢
                o.positionCS = TransformWorldToHClip(pWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target{
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
