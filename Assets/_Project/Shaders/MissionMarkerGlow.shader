Shader "Project Sparks/Mission Marker Glow"
{
    Properties
    {
        [HDR] _BaseColor("Цвет маркера", Color) = (2, 1.25, 0.08, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "MarkerGlow"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half distanceFromCenter = abs(input.uv.y * 2.0h - 1.0h);
                half core = 1.0h - smoothstep(0.12h, 0.3h, distanceFromCenter);
                half glow = saturate(1.0h - distanceFromCenter);
                half4 color = _BaseColor * input.color;
                color.a *= saturate(core + glow * glow * 0.35h);
                return color;
            }
            ENDHLSL
        }
    }
}
