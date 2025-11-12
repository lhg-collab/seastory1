Shader "URP/PortalOutline"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _EmissionColor ("Emission Color", Color) = (0.2,0.9,1,1)
        _EmissionStrength ("Emission Strength", Range(0,5)) = 1

        _OutlineColor ("Outline Color", Color) = (0.2,0.9,1,1)
        _OutlineWidth ("Outline Width (world units)", Range(0,0.1)) = 0.02
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100

        HLSLINCLUDE
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float  _EmissionStrength;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            v2f vert_main(appdata v)
            {
                v2f o;
                float4 posWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(posWS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag_main(v2f i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 emission = _EmissionColor.rgb * _EmissionStrength;
                return half4(albedo.rgb + emission, albedo.a);
            }

            // ---------- Outline pass (backface + normal extrusion in world space)
            struct v2f_out
            {
                float4 positionCS : SV_POSITION;
            };

            v2f_out vert_outline(appdata v)
            {
                v2f_out o;

                // world pos & normal
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float3 pWS = TransformObjectToWorld(v.positionOS).xyz;

                // extrude along normal in world space
                pWS += nWS * _OutlineWidth;

                o.positionCS = TransformWorldToHClip(pWS);
                return o;
            }

            half4 frag_outline(v2f_out i) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1);
            }
        ENDHLSL

        // Pass 0 : main (unlit + emission)
        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
                #pragma vertex   vert_main
                #pragma fragment frag_main
            ENDHLSL
        }

        // Pass 1 : outline (draw backfaces, slightly expanded)
        Pass
        {
            Name "Outline"
            Tags{ "LightMode"="SRPDefaultUnlit" }
            Cull Front            // draw backfaces
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
                #pragma vertex   vert_outline
                #pragma fragment frag_outline
            ENDHLSL
        }
    }

    FallBack Off
}
