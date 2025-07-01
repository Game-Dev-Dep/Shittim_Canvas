Shader "DSFX/FX_SHADER_Additive_0"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _Texture("Texture", 2D) = "white" {}
        [Toggle]_Custom_Data_Offset_Use("Custom_Data_Offset_Use", Float) = 0
        [Toggle]_ZWrite_Mode("ZWrite_Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_Cull_Mode("Cull_Mode", Float) = 2
        _ZOffsetFactor("ZOffsetFactor", Float) = 0
        _ZOffsetUnits("ZOffsetUnits", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest_Mode("ZTest_Mode", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Pass
        {
            Blend One One  // Additive blending
            Cull [_Cull_Mode]
            ZWrite [_ZWrite_Mode]
            ZTest [_ZTest_Mode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _CUSTOM_DATA_OFFSET_USE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 customData1 : TEXCOORD1;  // ��Ӧv1
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uvData : TEXCOORD0;  // xy: baseUV, zw: originalUV
                float4 customData : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Texture_ST;
                float _Custom_Data_Offset_Use;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // UV����
                output.uvData.xy = TRANSFORM_TEX(input.uv, _Texture);  // Ӧ��ST�任
                output.uvData.zw = input.uv;  // ����ԭʼUV
                
                // �����Զ�������
                output.customData = input.customData1;
                output.color = input.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // UVƫ�Ƽ���
                float2 baseUV = input.uvData.xy;
                float2 offsetUV = baseUV + input.customData.xy;  // ʹ��customData��xy������Ϊƫ��
                
                #ifdef _CUSTOM_DATA_OFFSET_USE_ON
                    float2 finalUV = offsetUV;
                #else
                    float2 finalUV = baseUV;
                #endif

                // �������
                half4 tex = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, finalUV);
                
                // ��ɫ���
                half3 result = tex.rgb;
                result *= input.color.rgb;  // ������ɫ���
                result *= _Color.rgb;      // HDR��ɫ��ǿ
                result *= input.color.a;   // ����Alpha����
                result *= tex.a;           // ����Alpha����

                // ������������AlphaΪ1.0��
                return half4(result, 1.0);

                
            }
            ENDHLSL
        }
    }
}