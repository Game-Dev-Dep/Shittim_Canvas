Shader "DSFX/FX_SHADER_AlwaysView_Add"
{
    Properties
    {
        _FX_TEX_FocusLine ("FX_TEX_FocusLine", 2D) = "white" {}
        _TextureSample0 ("Texture Sample 0", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        [HideInInspector] _texcoord ("", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One  // Additive blending

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_FX_TEX_FocusLine);
            SAMPLER(sampler_FX_TEX_FocusLine);
            TEXTURE2D(_TextureSample0);
            SAMPLER(sampler_TextureSample0);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FX_TEX_FocusLine_ST;
                float4 _TextureSample0_ST;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _TextureSample0);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample alpha mask texture
                float alphaMask = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, 
                    TRANSFORM_TEX(input.uv, _TextureSample0)).w;
                
                // Sample main texture with HDR color
                float4 mainTex = SAMPLE_TEXTURE2D(_FX_TEX_FocusLine, sampler_FX_TEX_FocusLine, 
                    TRANSFORM_TEX(input.uv, _FX_TEX_FocusLine));
                mainTex *= _Color;
                
                // Combine results with vertex color
                mainTex.xyz *= input.color.rgb;
                mainTex.w = alphaMask * mainTex.a * input.color.a;
                
                return mainTex;
            }
            ENDHLSL
        }
    }
}
