Shader "FX/FX_SHADER_Lobby_Order" 
{
    Properties 
    {
        _TextureSample0 ("Texture Sample 0", 2D) = "white" {}
        [HDR] _Color0 ("Color 0", Color) = (0.5,0.5,0.5,1)
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

            TEXTURE2D(_TextureSample0);
            SAMPLER(sampler_TextureSample0);

            CBUFFER_START(UnityPerMaterial)
                float4 _TextureSample0_ST;
                float4 _Color0;
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
                if (input.uv.x < 0.01 || input.uv.x > 1.0 || input.uv.y < 0.0 || input.uv.y > 1.0)
                {
                    // 超出范围返回透明
                    return half4(0, 0, 0, 0);
                }

                // 基础纹理采样
                half4 texColor = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, input.uv);
                
                // 固定使用R通道作为透明度源
                float alphaSource = texColor.a;

                // 颜色计算（保持原始强度逻辑）
                half3 finalColor = texColor.rgb * _Color0.rgb * input.color.rgb;

                // 透明度计算（R通道 * 顶点色Alpha * 颜色参数Alpha）
                half alpha = alphaSource * input.color.a;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
