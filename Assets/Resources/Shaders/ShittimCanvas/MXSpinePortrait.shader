Shader "Spine/Skeleton MX Portrait"
{
    Properties
    {
        [HDR] _Color ("Tint (_Color)", Color) = (1,1,1,1)
        _Black ("Black Point (_Black)", Vector) = (0,0,0,0)
        [NoScaleOffset] _MainTex ("Main Tex", 2D) = "black" {}
        // ����������HLSL��δʹ�ã���Ϊ���������ʾ
        _ColorFilterR ("GradingColorR", Vector) = (1,1,1,1)
        _ColorFilterG ("GradingColorG", Vector) = (1,1,1,1)
        _ColorFilterB ("GradingColorB", Vector) = (1,1,1,1)
        _ColorIntensity ("ColorIntensity", Float) = 1
        _ThresholdParams ("ThresholdParams", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float3 _Black;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // ��ȷ��ȡȫ�� mip bias
                float mipBias = 0.0;
                #if defined(UNITY_COMPILER_HLSL)
                    mipBias = unity_GlobalMipBias.x;
                #endif
                
                // ����������
                float4 texColor = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, i.uv, mipBias);
                
                // ��ɫ���� (1 - RGB)
                float3 invertedColor = 1.0 - texColor.rgb;
                
                // ������ɫ���
                float4 baseColor = texColor * _Color;
                
                // Alphaͨ������
                float finalAlpha = texColor.a * i.color.a * _Color.a;
                
                // ��ɫ����
                float3 blendedColor = (invertedColor * _Black) + baseColor.rgb;
                
                // Ӧ�ö�����ɫ
                blendedColor *= i.color.rgb;
                
                // ������ɫ�ϳ�
                return float4(baseColor.www * blendedColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}