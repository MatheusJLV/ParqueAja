Shader "URP/Custom/Lit_LensHide"
{
    Properties
    {
        _BaseMap     ("Base Map", 2D) = "white" {}
        _BaseColor   ("Base Color", Color) = (1,1,1,1)

        // Simple lighting controls
        _SpecColor   ("Specular Color", Color) = (0.2,0.2,0.2,1)
        _Smoothness  ("Smoothness (0-1)", Range(0,1)) = 0.5
        _AmbientBoost("Ambient Boost", Range(0,2)) = 1.0

        // Optional stencil (off by default in material)
        [Toggle(_USE_STENCIL)] _UseStencil ("Use Stencil Gate", Float) = 0
        _StencilRef ("Stencil Ref", Int) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        ZWrite On
        ZTest LEqual
        Cull Back
        // no Blend
        Stencil { Ref 1 Comp NotEqual Pass Keep }

        // Stencil: default hides inside the lens (NotEqual).
        // Swap to Equal if you want "only visible through the lens".
        Stencil
        {
            Ref  [_StencilRef]
            Comp NotEqual
            Pass Keep
        }

        Pass
        {
            Name "ForwardLitSimple"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP core + light helpers (safe across versions)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Optional keyword – keeps a “no-stencil” and “stencil” variant around if you want to use it
            #pragma multi_compile _ _USE_STENCIL

            // Textures/Samplers (self-declared)
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SpecColor;
                float4 _BaseMap_ST;
                float  _Smoothness;
                float  _AmbientBoost;
                int    _StencilRef;  // kept for SRP Batcher even if stencil disabled
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.normalWS    = NormalizeNormalPerVertex(nrm.normalWS);
                OUT.uv          = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return OUT;
            }

            // Simple Blinn-Phong lighting
            half4 frag(Varyings IN) : SV_Target
            {
                // Sample base color
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo  = baseTex.rgb * _BaseColor.rgb;
                half  alpha   = baseTex.a   * _BaseColor.a; // kept in case you later switch to alpha clip/transparent

                // Normalized directions
                half3 n = normalize(IN.normalWS);
                half3 v = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                // Main directional light (color, direction)
                Light mainLight = GetMainLight();
                half3 l = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;

                // Diffuse (Lambert)
                half NdotL = saturate(dot(n, -l));   // mainLight.direction points *from* light, hence -l
                half3 diffuse = albedo * lightColor * NdotL;

                // Specular (Blinn-Phong)
                half3 h = normalize(-l + v);
                // Convert smoothness [0..1] to a shininess exponent (rough heuristic)
                half shininess = max(1.0h, 1.0h + _Smoothness * 200.0h);
                half NdotH = saturate(dot(n, h));
                half3 spec = _SpecColor.rgb * lightColor * pow(NdotH, shininess);

                // Ambient from SH
                half3 ambient = SampleSH(n) * albedo * _AmbientBoost;

                half3 color = ambient + diffuse + spec;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

