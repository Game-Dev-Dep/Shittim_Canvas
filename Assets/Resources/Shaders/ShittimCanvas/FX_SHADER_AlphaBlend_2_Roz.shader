Shader "DSFX/FX_SHADER_AlphaBlend_2_Roz" 
{
    Properties 
    {
        _Texture ("Texture", 2D) = "white" {}
        [HDR]_Color0 ("Color 0", Color) = (0,0,0,0)
        _Float0 ("Intensity", Float) = 1
        [HideInInspector] _texcoord ("", 2D) = "white" {}
    }

    SubShader 
    {
        Tags 
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass 
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

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

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _Color0;
                float _Float0;
            CBUFFER_END

            Varyings vert(Attributes input) 
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _Texture);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target 
            {
                // 纹理采样
                half4 texColor = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
                
                // 颜色计算流程
                half4 result = texColor * input.color;  // 顶点色混合
                result *= _Color0;                      // HDR颜色混合
                result.rgb *= _Float0;                  // 强度控制

                return result;
            }
            ENDHLSL
        }
    }
}