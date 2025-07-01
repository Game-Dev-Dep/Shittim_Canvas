Shader "DSFX/FX_SHADER_Matcap_0"
{
    Properties
    {
        [HDR]_Main_Color("Main Color", Color) = (1,1,1,1)
        _Multiply("Intensity Multiplier", Float) = 1
        _Main_Tex("Main Texture", 2D) = "white" {}
        _Matcap_Tex("Matcap Texture", 2D) = "white" {}
        [Toggle]_ZWrite_Mode("ZWrite Mode", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest_Mode("ZTest Mode", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)]_Cull_Mode("Cull Mode", Float) = 2
        [HideInInspector]_texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull_Mode]
            ZWrite [_ZWrite_Mode]
            ZTest [_ZTest_Mode]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalVS : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Main_Tex_ST;
                float4 _Main_Color;
                float _Multiply;
                float _ZWrite_Mode;
                float _ZTest_Mode;
                float _Cull_Mode;
            CBUFFER_END

            TEXTURE2D(_Main_Tex);
            SAMPLER(sampler_Main_Tex);
            TEXTURE2D(_Matcap_Tex);
            SAMPLER(sampler_Matcap_Tex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Transform position to clip space
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // Main texture UV with ST transform
                output.uv = TRANSFORM_TEX(input.uv, _Main_Tex);
                
                // Transform normal to view space
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalVS = TransformWorldToViewDir(normalWS);
                
                // Pass vertex color
                output.color = input.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Sample main texture
                half4 mainTex = SAMPLE_TEXTURE2D(_Main_Tex, sampler_Main_Tex, input.uv);
                half3 baseColor = mainTex.rgb * _Main_Color.rgb * input.color.rgb;
                
                // Generate matcap UV
                float2 matcapUV = input.normalVS.xy * 0.5 + 0.5;
                
                // Sample matcap texture
                half4 matcap = SAMPLE_TEXTURE2D(_Matcap_Tex, sampler_Matcap_Tex, matcapUV);
                
                // Combine colors with multiplier
                half3 finalColor = baseColor * matcap.rgb * _Multiply;
                
                // Alpha calculation
                half alpha = mainTex.a * matcap.a * _Main_Color.a * input.color.a;
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}