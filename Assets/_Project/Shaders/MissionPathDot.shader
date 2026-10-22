Shader "Project Sparks/Mission Path Dot"
{
    Properties
    {
        _BaseColor("Цвет точки", Color) = (0, 1, 0.9559734, 1)
        _OutlineColor("Цвет обводки", Color) = (0, 0.1946178, 0.1878208, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "PathDot"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
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
                float distanceFromCenter = length(input.uv * 2.0 - 1.0);
                float edgeWidth = max(fwidth(distanceFromCenter), 0.002);
                half fill = 1.0 - smoothstep(0.74 - edgeWidth, 0.74 + edgeWidth, distanceFromCenter);
                half opacity = 1.0 - smoothstep(0.92 - edgeWidth, 0.92 + edgeWidth, distanceFromCenter);
                half4 color = lerp(_OutlineColor, _BaseColor, fill);
                color.a *= opacity;
                return color;
            }
            ENDHLSL
        }
    }
}
