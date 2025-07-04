Shader "DSFX/FX_SHADER_Step_0_MXChar" {
    Properties {
        [HDR] _Color ("Color", Vector) = (1,1,1,1)
        _Multiply ("Multiply", Float) = 1
        _Tex_Main ("Tex_Main", 2D) = "white" {}
        [Toggle] _RGBRGBA ("RGB>RGBA", Float) = 0
        [Toggle] _Custom_Data_Main_Offset_Use ("Custom_Data_Main_Offset_Use", Float) = 0
        _Main_Speed_X ("Main_Speed_X", Float) = 0
        _Main_Speed_Y ("Main_Speed_Y", Float) = 0
        _Tex_Mask ("Tex_Mask", 2D) = "white" {}
        [Toggle] _Custom_Data_Mask_Offset_Use ("Custom_Data_Mask_Offset_Use", Float) = 0
        _Mask_Speed_X ("Mask_Speed_X", Float) = 0
        _Mask_Speed_Y ("Mask_Speed_Y", Float) = 0
        [Toggle] _Step_Scroll_Use ("Step_Scroll_Use", Float) = 1
        _Step_Power ("Step_Power", Range(0, 1)) = 1
        [Toggle] _Step_Custom_DataVertex_color_Use ("Step_Custom_Data/Vertex_color_Use", Float) = 0
        [Toggle] _Vertex_Alpha_Use ("Vertex_Alpha_Use", Float) = 0
        [Toggle] _ZWrite_Mode ("ZWrite_Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull_Mode ("Cull_Mode", Float) = 2
        _ZOffsetFactor ("ZOffsetFactor", Float) = 0
        _ZOffsetUnits ("ZOffsetUnits", Float) = 0
        [Space] [Toggle(_DITHER_HORIZONTAL_LINES)] _EnableDither ("*DITHER_HORIZONTAL_LINES", Float) = 0
        _DitherThreshold ("*DitherThreshold", Range(0, 1)) = 0
        [Toggle(_GRAYSCALE_MODE)] _EnableGrayscale ("*GRAYSCALE_MODE", Float) = 0
        _GrayBrightness ("*GrayBrightness", Range(0, 2)) = 1
    }

    SubShader {
        Tags { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass {
            Blend SrcAlpha One
            ZWrite [_ZWrite_Mode]
            Cull [_Cull_Mode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _DITHER_HORIZONTAL_LINES
            #pragma multi_compile_fragment _ _GRAYSCALE_MODE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _ZWrite_Mode;
                float _Cull_Mode;
                float _ZOffsetFactor;
                float _ZOffsetUnits;
                float4 _Tex_Main_ST;
                float _Main_Speed_X;
                float _Main_Speed_Y;
                float _Custom_Data_Main_Offset_Use;
                float _Step_Scroll_Use;
                float _Step_Power;
                float _Step_Custom_DataVertex_color_Use;
                float4 _Tex_Mask_ST;
                float _Mask_Speed_X;
                float _Mask_Speed_Y;
                float _Custom_Data_Mask_Offset_Use;
                float4 _Color;
                float _Multiply;
                float _RGBRGBA;
                float _Vertex_Alpha_Use;
                float _DitherThreshold;
                float _GrayBrightness;
            CBUFFER_END

            TEXTURE2D(_Tex_Main);
            SAMPLER(sampler_Tex_Main);
            TEXTURE2D(_Tex_Mask);
            SAMPLER(sampler_Tex_Mask);

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 customData1 : TEXCOORD1;
                float4 customData2 : TEXCOORD2;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 customData1 : TEXCOORD1;
                float4 customData2 : TEXCOORD2;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input) {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.customData1 = input.customData1;
                output.customData2 = input.customData2;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Calculate mask UV with animation
                float2 maskUV = input.uv * _Tex_Mask_ST.xy + _Tex_Mask_ST.zw;
                maskUV += _Time.yy * float2(_Mask_Speed_X, _Mask_Speed_Y);
                
                // Add custom data offset for mask if enabled
                float2 maskOffset = _Custom_Data_Mask_Offset_Use > 0.5 ? input.customData2.xy : float2(0, 0);
                maskUV += maskOffset;
                
                // Sample mask texture
                float maskValue = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, maskUV).r;
                
                // Calculate step threshold
                float stepThreshold;
                if (_Step_Custom_DataVertex_color_Use > 0.5) {
                    stepThreshold = 1.0 - input.color.a;
                } else {
                    stepThreshold = input.customData1.x;
                }
                
                // Use step scroll or fixed power
                if (_Step_Scroll_Use > 0.5) {
                    stepThreshold = stepThreshold;
                } else {
                    stepThreshold = _Step_Power;
                }
                
                // Apply step function
                float stepMask = maskValue >= stepThreshold ? 1.0 : 0.0;
                
                // Calculate main texture UV with animation
                float2 mainUV = input.uv * _Tex_Main_ST.xy + _Tex_Main_ST.zw;
                mainUV += _Time.yy * float2(_Main_Speed_X, _Main_Speed_Y);
                
                // Add custom data offset for main texture if enabled
                float2 mainOffset = _Custom_Data_Main_Offset_Use > 0.5 ? input.customData1.zw : float2(0, 0);
                mainUV += mainOffset;
                
                // Sample main texture
                half4 mainTex = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, mainUV);
                
                // Calculate final color
                half3 finalColor = mainTex.rgb * input.color.rgb;
                finalColor *= stepMask;
                finalColor *= _Color.rgb;
                finalColor *= _Multiply;
                
                // Calculate alpha
                float finalAlpha = _RGBRGBA > 0.5 ? mainTex.a : mainTex.r;
                finalAlpha *= stepMask * _Color.a;
                
                float vertexAlpha = _Vertex_Alpha_Use > 0.5 ? 1.0 : input.color.a;
                finalAlpha *= vertexAlpha;
                finalAlpha *= _Multiply;
                finalAlpha = saturate(finalAlpha);
                
                half4 result = half4(finalColor * finalAlpha, 1);
                
                #ifdef _GRAYSCALE_MODE
                    float gray = dot(result.rgb, float3(0.299, 0.587, 0.114));
                    result.rgb = gray * _GrayBrightness;
                #endif
                
                #ifdef _DITHER_HORIZONTAL_LINES
                    float2 screenPos = input.positionCS.xy;
                    float dither = fmod(floor(screenPos.y), 2.0);
                    if (dither < _DitherThreshold) {
                        discard;
                    }
                #endif
                
                return result;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
