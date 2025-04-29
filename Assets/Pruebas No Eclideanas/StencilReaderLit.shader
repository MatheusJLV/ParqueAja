Shader "Custom/StencilReaderLit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        Stencil
        {
            Ref 1
            Comp equal
            Pass keep
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            float4 _BaseColor;
            float _Metallic;
            float _Smoothness;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Fetch lighting
                half3 normal = normalize(input.normalWS);
                half3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);

                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;

                // Simple diffuse Lambert lighting
                half NdotL = max(dot(normal, lightDir), 0.0);
                half3 diffuse = _BaseColor.rgb * lightColor.rgb * NdotL;

                // Optional basic fresnel boost
                half fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 5.0);
                half3 fresnelColor = _BaseColor.rgb * fresnel * 0.1;

                half3 finalColor = diffuse + fresnelColor;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
