Shader "DSFX/FX_SHADER_Step_0"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1,1,1,1)
        _Multiply("Multiply", Float) = 1
        _Tex_Main("Tex_Main", 2D) = "white" {}
        [Toggle]_RGBRGBA("RGB>RGBA", Float) = 0
        [Toggle]_Custom_Data_Main_Offset_Use("Custom_Data_Main_Offset_Use", Float) = 0
        _Main_Speed_X("Main_Speed_X", Float) = 0
        _Main_Speed_Y("Main_Speed_Y", Float) = 0
        _Tex_Mask("Tex_Mask", 2D) = "white" {}
        [Toggle]_Custom_Data_Mask_Offset_Use("Custom_Data_Mask_Offset_Use", Float) = 0
        _Mask_Speed_X("Mask_Speed_X", Float) = 0
        _Mask_Speed_Y("Mask_Speed_Y", Float) = 0
        [Toggle]_Step_Scroll_Use("Step_Scroll_Use", Float) = 1
        _Step_Power("Step_Power", Range(0, 1)) = 1
        [Toggle]_Step_Custom_DataVertex_color_Use("Step_Custom_Data/Vertex_color_Use", Float) = 0
        [Toggle]_Vertex_Alpha_Use("Vertex_Alpha_Use", Float) = 0
        [Toggle]_ZWrite_Mode("ZWrite_Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest_Mode("ZTest_Mode", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)]_Cull_Mode("Cull_Mode", Float) = 2
        _ZOffsetFactor("ZOffsetFactor", Float) = 0
        _ZOffsetUnits("ZOffsetUnits", Float) = 0
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
            Blend SrcAlpha One
            Cull [_Cull_Mode]
            ZWrite [_ZWrite_Mode]
            ZTest [_ZTest_Mode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _CUSTOM_DATA_MASK_OFFSET_USE_ON
            #pragma shader_feature_local _STEP_SCROLL_USE_ON
            #pragma shader_feature_local _STEP_CUSTOM_DATAVERTEX_COLOR_USE_ON
            #pragma shader_feature_local _CUSTOM_DATA_MAIN_OFFSET_USE_ON
            #pragma shader_feature_local _VERTEX_ALPHA_USE_ON
            #pragma shader_feature_local _RGBRGBA_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 customData1 : TEXCOORD1; // 对应原始v1
                float4 customData2 : TEXCOORD2; // 对应原始v2
                float4 customData3 : TEXCOORD3; // 对应原始v3
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 customData1 : TEXCOORD1;
                float4 color : COLOR;
                float4 customData2 : TEXCOORD2;
                float4 customData3 : TEXCOORD3; // 新增通道
            };

            TEXTURE2D(_Tex_Main);
            SAMPLER(sampler_Tex_Main);
            TEXTURE2D(_Tex_Mask);
            SAMPLER(sampler_Tex_Mask);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Tex_Main_ST;
                float4 _Tex_Mask_ST;
                float _Multiply;
                float _Main_Speed_X;
                float _Main_Speed_Y;
                float _Mask_Speed_X;
                float _Mask_Speed_Y;
                float _Step_Power;
                float _ZWrite_Mode;
                float _Custom_Data_Mask_Offset_Use;
                float _Step_Scroll_Use;
                float _Step_Custom_DataVertex_color_Use;
                float _Custom_Data_Main_Offset_Use;
                float _RGBRGBA;
                float _Vertex_Alpha_Use;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 显式初始化所有成员
                ZERO_INITIALIZE(Varyings, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // 完整初始化uv结构体
                output.uv.xy = input.uv;
                output.uv.zw = float2(0,0);
                
                // 传递所有自定义数据
                output.customData1 = input.customData1;  // 对应原始v1
                output.customData2 = input.customData2;  // 对应原始v2
                output.customData3 = input.customData3;  // 对应原始v3
                
                output.color = input.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // [1] Mask处理 ------------------------
                float2 uvMask = input.uv.xy * _Tex_Mask_ST.xy + _Tex_Mask_ST.zw;
                uvMask += _Time.y * float2(_Mask_Speed_X, _Mask_Speed_Y);
    
                #ifdef _CUSTOM_DATA_MASK_OFFSET_USE_ON
                    uvMask += input.customData2.xy; // 使用XY而非ZW
                #endif
    
                float maskValue = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, uvMask).r;

                // [2] Step计算 ------------------------
                float stepThreshold = lerp(_Step_Power, 1.0 - input.color.a, 
                                        _Step_Custom_DataVertex_color_Use);
                stepThreshold = lerp(stepThreshold, input.customData1.x, 
                                        _Step_Scroll_Use);
                float stepResult = step(stepThreshold, maskValue);

                // [3] 主纹理处理 ----------------------
                float2 uvMain = input.uv.xy * _Tex_Main_ST.xy + _Tex_Main_ST.zw;
                uvMain += _Time.y * float2(_Main_Speed_X, _Main_Speed_Y);
    
                #ifdef _CUSTOM_DATA_MAIN_OFFSET_USE_ON
                    uvMain += input.customData1.zw;
                #endif
    
                half4 mainTex = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, uvMain);

                // [4] 颜色混合 ------------------------
                #ifdef _RGBRGBA_ON
                    float alphaSource = mainTex.a;
                #else
                    float alphaSource = mainTex.r;
                #endif

                #ifdef _VERTEX_ALPHA_USE_ON
                    float alphaMultiplier = input.color.a;
                #else
                    float alphaMultiplier = 1.0;
                #endif

                half3 finalColor = mainTex.rgb * input.color.rgb;
                finalColor *= stepResult; 
                finalColor *= _Color.rgb;
                finalColor *= alphaSource; // 新增关键步骤
                finalColor *= _Multiply;

                // [5] 透明度计算 ----------------------
                half finalAlpha = stepResult * _Color.a;
                finalAlpha *= alphaSource * alphaMultiplier;
                finalAlpha *= _Multiply; // 最后应用全局强度
                finalAlpha = saturate(finalAlpha);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}