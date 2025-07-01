Shader "DSFX/FX_SHADER_Step_Distort_0"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _Multiply("Multiply", Float) = 1
        _Tex_Main("Tex_Main", 2D) = "white" {}
        [Toggle]_RGBRGBA("RGB>RGBA", Float) = 0
        [Toggle]_Custom_Data_Main_Offset_Use("Custom_Data_Main_Offset_Use", Float) = 0
        [Toggle]_Custom_Data_Mask_Offset_Usecustom78("Custom_Data_Mask_Offset_Use", Float) = 0
        _Main_Speed_X("Main_Speed_X", Float) = 0
        _Main_Speed_Y("Main_Speed_Y", Float) = 0
        _Tex_Mask("Tex_Mask", 2D) = "white" {}
        _Mask_Speed_X("Mask_Speed_X", Float) = 0
        _Mask_Speed_Y("Mask_Speed_Y", Float) = 0
        _Tex_Distort("Tex_Distort", 2D) = "white" {}
        _Dis_Speed_X("Dis_Speed_X", Float) = 0
        _Dis_Speed_Y("Dis_Speed_Y", Float) = 0
        _Distortion_Power_X("Distortion_Power_X", Float) = 0
        _Distortion_Power_Y("Distortion_Power_Y", Float) = 0
        [Toggle]_Custom_Data_Distort_Power_Use("Custom_Data_Distort_Power_Use", Float) = 0
        [Toggle]_Step_Scroll_Use("Step_Scroll_Use", Float) = 1
        _Step_Power("Step_Power", Range(0, 1)) = 1
        [Toggle]_Step_Custom_DataVertex_color_Use("Step_Custom_Data/Vertex_color_Use", Float) = 0
        [Toggle]_Vertex_Alpha_Use("Vertex_Alpha_Use", Float) = 0
        [Toggle]_UV_Add_Use("UV_Add_Use", Float) = 0
        _UV_Add_TilingOffset("UV_Add_Tiling/Offset", Vector) = (0,0,0,0)
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
            #pragma shader_feature_local _CUSTOM_DATA_MASK_OFFSET_USECUSTOM78_ON
            #pragma shader_feature_local _STEP_SCROLL_USE_ON
            #pragma shader_feature_local _STEP_CUSTOM_DATAVERTEX_COLOR_USE_ON
            #pragma shader_feature_local _CUSTOM_DATA_MAIN_OFFSET_USE_ON
            #pragma shader_feature_local _VERTEX_ALPHA_USE_ON
            #pragma shader_feature_local _RGBRGBA_ON
            #pragma shader_feature_local _CUSTOM_DATA_DISTORT_POWER_USE_ON
            #pragma shader_feature_local _UV_ADD_USE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 customData1 : TEXCOORD1; // v1
                float4 customData2 : TEXCOORD2; // v2
                float4 customData3 : TEXCOORD3; // v3
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 customData1 : TEXCOORD1;
                float4 customData2 : TEXCOORD2;
                float4 color : COLOR;
            };

            TEXTURE2D(_Tex_Main);
            SAMPLER(sampler_Tex_Main);
            TEXTURE2D(_Tex_Mask);
            SAMPLER(sampler_Tex_Mask);
            TEXTURE2D(_Tex_Distort);
            SAMPLER(sampler_Tex_Distort);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Tex_Main_ST;
                float4 _Tex_Mask_ST;
                float4 _Tex_Distort_ST;
                float4 _UV_Add_TilingOffset;
                float _Multiply;
                float _Main_Speed_X;
                float _Main_Speed_Y;
                float _Mask_Speed_X;
                float _Mask_Speed_Y;
                float _Dis_Speed_X;
                float _Dis_Speed_Y;
                float _Distortion_Power_X;
                float _Distortion_Power_Y;
                float _Step_Power;
                float _ZWrite_Mode;
                float _Custom_Data_Mask_Offset_Usecustom78;
                float _Step_Scroll_Use;
                float _Step_Custom_DataVertex_color_Use;
                float _Custom_Data_Main_Offset_Use;
                float _RGBRGBA;
                float _Vertex_Alpha_Use;
                float _UV_Add_Use;
                float _Custom_Data_Distort_Power_Use;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv.xy = input.uv;
                output.customData1 = input.customData1;
                output.customData2 = input.customData2;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 扭曲纹理处理
                float2 uvDistort = input.uv.xy * _Tex_Distort_ST.xy + _Tex_Distort_ST.zw;
                float2 distortSpeed = _Time.y * float2(_Dis_Speed_X, _Dis_Speed_Y);
                float2 distortUV = uvDistort + distortSpeed;
                float2 distortValue = SAMPLE_TEXTURE2D(_Tex_Distort, sampler_Tex_Distort, distortUV).rg;

                #ifdef _CUSTOM_DATA_DISTORT_POWER_USE_ON
                    float2 distortPower = input.customData2.xy;
                #else
                    float2 distortPower = float2(_Distortion_Power_X, _Distortion_Power_Y);
                #endif

                // 遮罩纹理处理
                float2 uvMask = input.uv.xy * _Tex_Mask_ST.xy + _Tex_Mask_ST.zw;
                uvMask += _Time.y * float2(_Mask_Speed_X, _Mask_Speed_Y);
                
                // UV叠加计算
                #ifdef _UV_ADD_USE_ON
                    float2 uvAdd = input.uv.xy * _UV_Add_TilingOffset.xy + _UV_Add_TilingOffset.zw;
                    float uvAddFactor = uvAdd.x + uvAdd.y;
                #else
                    float uvAddFactor = 1.0;
                #endif

                // 应用扭曲
                float2 finalDistort = distortValue * distortPower * uvAddFactor;
                uvMask += finalDistort;

                #ifdef _CUSTOM_DATA_MASK_OFFSET_USECUSTOM78_ON
                    uvMask += input.customData2.zw;
                #endif

                // 遮罩采样
                float mask = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, uvMask).r;

                // 步骤计算
                float stepThreshold = lerp(_Step_Power, 1.0 - input.color.a, 
                    _Step_Custom_DataVertex_color_Use);
                stepThreshold = lerp(stepThreshold, input.customData1.x, 
                    _Step_Scroll_Use);
                float stepResult = step(stepThreshold, mask);

                // 主纹理处理
                float2 uvMain = input.uv.xy * _Tex_Main_ST.xy + _Tex_Main_ST.zw;
                uvMain += _Time.y * float2(_Main_Speed_X, _Main_Speed_Y);
                uvMain += finalDistort;

                #ifdef _CUSTOM_DATA_MAIN_OFFSET_USE_ON
                    uvMain += input.customData1.zw;
                #endif

                // 主纹理采样
                half4 mainTex = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, uvMain);

                // 颜色合成
                half3 finalColor = mainTex.rgb * input.color.rgb * _Color.rgb * _Multiply;
                finalColor *= stepResult;

                // Alpha计算
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

                half finalAlpha = _Color.a * stepResult * alphaSource * alphaMultiplier * _Multiply;
                finalAlpha = saturate(finalAlpha);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}