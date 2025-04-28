Shader "Custom/StencilBlocker"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+2" }

        Stencil
        {
            Ref 1
            Comp equal
            Pass keep
        }

        Pass
        {
            Name "VoidBlocker"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 1); // Solid black
            }
            ENDHLSL
        }
    }
}
