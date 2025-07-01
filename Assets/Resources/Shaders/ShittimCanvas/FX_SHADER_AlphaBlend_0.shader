Shader "DSFX/FX_SHADER_AlphaBlend_0"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _Multiply("Multiply", Float) = 1
        _Texture("Texture", 2D) = "white" {}
        [Toggle]_RGBRGBA("RGB>RGBA", Float) = 1
        [Toggle]_Main_Texture_No("White Mode", Float) = 0
        [Toggle]_Custom_Data_Offset_Use("Use UV Offset", Float) = 0
        [Toggle]_ZWrite_Mode("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_Cull_Mode("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest_Mode("ZTest", Float) = 4
        _ZOffsetFactor("Z Offset Factor", Float) = 0
        _ZOffsetUnits("Z Offset Units", Float) = 0
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
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull_Mode]
            ZWrite [_ZWrite_Mode]
            ZTest [_ZTest_Mode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _CUSTOM_DATA_OFFSET_USE_ON
            #pragma shader_feature_local _MAIN_TEXTURE_NO_ON
            #pragma shader_feature_local _RGBRGBA_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 customData1 : TEXCOORD1; // ��Ӧv1
                float4 customData2 : TEXCOORD2; // ��Ӧv2
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uvData : TEXCOORD0;  // xy: baseUV, zw: offsetUV
                float4 customData : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Texture_ST;
                float _Multiply;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // ����UV����
                output.uvData.xy = TRANSFORM_TEX(input.uv, _Texture);
                
                // �Զ���ƫ�ƴ���
                output.uvData.zw = input.uv + input.customData1.xy;
                
                output.customData = input.customData2;
                output.color = input.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // UV��ϼ���
                #ifdef _CUSTOM_DATA_OFFSET_USE_ON
                    float2 finalUV = input.uvData.zw;
                #else
                    float2 finalUV = input.uvData.xy;
                #endif

                // �������
                half4 tex = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, finalUV);

                // ��ģģʽ
                #ifdef _MAIN_TEXTURE_NO_ON
                    half3 baseColor = half3(1,1,1);
                #else
                    half3 baseColor = tex.rgb;
                #endif

                // ��ɫ���
                half3 finalColor = baseColor;
                finalColor *= _Color.rgb;         // HDR��ɫ
                finalColor *= input.color.rgb;     // ������ɫ
                finalColor *= _Multiply;          // ȫ��ǿ��

                // Alphaͨ��ѡ��
                #ifdef _RGBRGBA_ON
                    half alpha = tex.a;
                #else
                    half alpha = tex.r;
                #endif

                // ͸���ȼ���
                half finalAlpha = alpha;
                finalAlpha *= input.color.a;      // ����Alpha
                finalAlpha *= _Color.a;           // ����ɫAlpha
                finalAlpha *= _Multiply;           // ȫ��ǿ��
                finalAlpha = saturate(finalAlpha);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}