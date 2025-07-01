Shader "DSFX/FX_SHADER_AlphaBlend_Add_Mask_0" {
    Properties {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _Multiply ("Multiply", Float) = 1
        _Tex_Main ("Tex_Main (RGB) Alpha (A)", 2D) = "white" {}
        [Toggle(_RGBRGBA_ON)] _RGBRGBA ("MainTex Alpha from Luminance (RGB > A)", Float) = 0
        _Main_Speed_X ("Main_Speed_X", Float) = 0
        _Main_Speed_Y ("Main_Speed_Y", Float) = 0
        [Toggle(_CUSTOM_DATA_OFFSET_ON)] _Custom_Data_MainMask_Offset_Use ("Use Custom UV Data for Offset", Float) = 0 // Mesh UV1 (xy for Main, zw for Mask)
        _Tex_Mask ("Tex_Mask (R channel)", 2D) = "white" {}
        _Mask_Speed_X ("Mask_Speed_X", Float) = 0
        _Mask_Speed_Y ("Mask_Speed_Y", Float) = 0

        // Render States
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend Mode", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend Mode", Float) = 1 // One (for Additive) or Ten (OneMinusSrcAlpha for AlphaBlend)
        [Enum(Off,0,On,1)] _ZWrite_Mode ("ZWrite Mode", Float) = 0 // Off for transparent
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest_Mode ("ZTest Mode", Float) = 4 // LEqual
        [Enum(UnityEngine.Rendering.CullMode)] _Cull_Mode ("Cull Mode", Float) = 2 // Back
        _ZOffsetFactor ("ZOffsetFactor", Float) = 0
        _ZOffsetUnits ("ZOffsetUnits", Float) = 0
    }
    SubShader {
        Tags {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "ShaderModel"="4.5" // For texture arrays if they were truly used, but here it's just two textures
        }
        LOD 100

        Pass {

            Blend [_SrcBlend] One
            ZWrite [_ZWrite_Mode]
            ZTest [_ZTest_Mode]
            Cull [_Cull_Mode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 // Exclude if using texture arrays explicitly, not an issue here
            #pragma vertex vert
            #pragma fragment frag

            // Shader keywords
            #pragma shader_feature_local _RGBRGBA_ON
            #pragma shader_feature_local _CUSTOM_DATA_OFFSET_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0; // Main UVs for _Tex_Main, _Tex_Mask
                float4 customData   : TEXCOORD1; // Custom data for offsets (v0 in original PS_INPUT)
                float4 color        : COLOR;     // Vertex Color (v2 in original PS_INPUT)
            };

            struct Varyings {
                float4 clipPos      : SV_POSITION;
                // v0 in original PS_INPUT (custom data for offsets)
                float4 mainMaskOffsetsRaw : TEXCOORD0; // Will be input.v0 in fragment
                // v1 in original PS_INPUT (main texture UVs)
                float2 uv           : TEXCOORD1;       // Will be input.v1.xy in fragment
                // v2 in original PS_INPUT (vertex color)
                float4 vertexColor  : TEXCOORD2;       // Will be input.v2 in fragment
            };

            // Material CBuffer
            CBUFFER_START(UnityPerMaterial)
                float4 _Tex_Main_ST;
                float4 _Color;
                float4 _Tex_Mask_ST;
                // _ZTest_Mode, _ZWrite_Mode, _ZOffsetUnits, _Cull_Mode, _ZOffsetFactor are handled by render states
                // _Custom_Data_MainMask_Offset_Use (handled by shader_feature)
                float _Main_Speed_X;
                float _Main_Speed_Y;
                // _RGBRGBA (handled by shader_feature)
                float _Mask_Speed_X;
                float _Mask_Speed_Y;
                float _Multiply;
            CBUFFER_END

            TEXTURE2D(_Tex_Main);       SAMPLER(sampler_Tex_Main);
            TEXTURE2D(_Tex_Mask);       SAMPLER(sampler_Tex_Mask);
            // The original used arrays: uniform Texture2D<float4> textures2D[2]
            // We define them explicitly. __Tex_Main was index 0, __Tex_Mask was index 1.

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                output.clipPos = positionInputs.positionCS;

                output.mainMaskOffsetsRaw = input.customData; // Passed as v0
                output.uv = input.uv;                         // Passed as v1.xy
                output.vertexColor = input.color;             // Passed as v2

                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Map Varyings to original PS_INPUT names for easier translation
                float4 _vs_TEXCOORD3_customOffsets = input.mainMaskOffsetsRaw; // v0
                float4 _vs_TEXCOORD4_mainUV_raw = float4(input.uv, 0, 0); // v1 (original was float4)
                float4 _vs_COLOR0_vertexColor = input.vertexColor; // v2

                // Original logic starts here, adapting variable names and functions
                // (_u_xlat0.xy = ((_vs_TEXCOORD4.xy * __Tex_Main_ST.xy) + __Tex_Main_ST.zw));
                float2 mainTexUV = _vs_TEXCOORD4_mainUV_raw.xy * _Tex_Main_ST.xy + _Tex_Main_ST.zw;

                // (_u_xlatb6 = !all(float4(0.0, 0.0, 0.0, 0.0) == vec4_ctor(__Custom_Data_MainMask_Offset_Use)));
                // float4 s1607 = {0,0,0,0}; if (bool_ctor(_u_xlatb6)) { (s1607 = _vs_TEXCOORD3); } else { (s1607 = float4(0.0,0.0,0.0,0.0)); } (_u_xlat1 = s1607);
                float4 customScrollOffsets = float4(0,0,0,0);
                #if _CUSTOM_DATA_OFFSET_ON
                    customScrollOffsets = _vs_TEXCOORD3_customOffsets;
                #endif
                
                // (_u_xlat6.xy = ((__Time.yy * vec2_ctor(__Main_Speed_X, __Main_Speed_Y)) + _u_xlat1.xy));
                float2 mainScroll = (_Time.yy * float2(_Main_Speed_X, _Main_Speed_Y)) + customScrollOffsets.xy;
                
                // (_u_xlat1.xy = ((__Time.yy * vec2_ctor(__Mask_Speed_X, __Mask_Speed_Y)) + _u_xlat1.zw));
                float2 maskScroll = (_Time.yy * float2(_Mask_Speed_X, _Mask_Speed_Y)) + customScrollOffsets.zw;

                // (_u_xlat0.xy = (_u_xlat0.xy + _u_xlat6.xy));
                mainTexUV += mainScroll;
                
                // (_u_xlat0 = gl_texture2D(__Tex_Main, _u_xlat0.xy));
                float4 mainTexSample = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, mainTexUV);

                // (_u_xlat2.xyz = (_u_xlat0.xyz * _vs_COLOR0.xyz));
                // (_u_xlat2.xyz = (_u_xlat2.xyz * __Color.xyz));
                float3 baseRGB = mainTexSample.rgb * _vs_COLOR0_vertexColor.rgb * _Color.rgb;
                
                // (_u_xlatb7 = !all(float4(0.0, 0.0, 0.0, 0.0) == vec4_ctor(__RGBRGBA)));
                // float3 s1608 = {0,0,0}; if (bool_ctor(_u_xlatb7)) { (s1608 = _u_xlat0.www); } else { (s1608 = _u_xlat0.xyz); } (_u_xlat0.xyz = s1608);
                float3 mainTexRGBcomponentSource = mainTexSample.rgb;
                #if _RGBRGBA_ON
                    mainTexRGBcomponentSource = mainTexSample.aaa; // Use alpha channel as grayscale for RGB effect
                #endif
                
                // (_u_xlat3.xyz = (_u_xlat0.xyz * _u_xlat2.xyz)); // Decompiler reused _u_xlat0.xyz which was s1608
                                                               // and _u_xlat2.xyz which was (mainTexSample.rgb * _vs_COLOR0.rgb * _Color.rgb)
                                                               // This effectively means: (mainTex.AAA or mainTex.RGB) * (mainTex.RGB * vColor.RGB * tint.RGB)
                                                               // This is a common pattern for "multiply by texture color, then modulate by tint and vertex color"
                                                               // Let's re-interpret the intent:
                                                               // Base color is mainTexSample.rgb * _vs_COLOR0_vertexColor.rgb * _Color.rgb
                                                               // Then this base color is further modulated by either mainTexSample.aaa (if _RGBRGBA_ON) or mainTexSample.rgb (if _RGBRGBA_OFF)
                                                               // This seems like double-dipping on mainTexSample.rgb if _RGBRGBA_OFF.
                                                               // More typical would be:
                                                               //   rgb = mainTexSample.rgb * _Color.rgb * _vs_COLOR0_vertexColor.rgb; (Initial color)
                                                               //   rgb *= _Tex_Mask.r (Masking)
                                                               //   rgb *= _Multiply (Intensity)
                                                               //   alpha derived separately.
                                                               // Let's stick to the decompiled logic translation as closely as possible first:
                float3 combinedRGB = mainTexRGBcomponentSource * baseRGB; // This is (mainTex.AAA or mainTex.RGB) * (mainTex.RGB * vColor.RGB * _Color.RGB)

                // (_u_xlat7.xy = ((_vs_TEXCOORD4.xy * __Tex_Mask_ST.xy) + __Tex_Mask_ST.zw));
                float2 maskTexUV = _vs_TEXCOORD4_mainUV_raw.xy * _Tex_Mask_ST.xy + _Tex_Mask_ST.zw;
                
                // (_u_xlat1.xy = (_u_xlat7.xy + _u_xlat1.xy)); // _u_xlat1.xy was maskScroll
                maskTexUV += maskScroll;
                
                // (_u_xlat16_1 = gl_texture2D(__Tex_Mask, _u_xlat1.xy).x);
                float maskValue = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, maskTexUV).r; // Assuming mask is in R channel

                // (_u_xlat3.xyz = (_u_xlat3.xyz * vec3_ctor(_u_xlat16_1));
                combinedRGB *= maskValue;

                // (_u_xlat2.xyz = (_u_xlat3.xyz * vec3_ctor(vec3_ctor(__Multiply, __Multiply, __Multiply))));
                combinedRGB *= _Multiply;

                // Alpha Calculation:
                // (_u_xlat3.x = (_vs_COLOR0.w * _vs_COLOR0.w));
                //float finalAlpha = _vs_COLOR0_vertexColor.a * _vs_COLOR0_vertexColor.a;
                float finalAlpha = _vs_COLOR0_vertexColor.a;

                // (_u_xlat0.x = (_u_xlat3.x * _u_xlat0.x)); 
                //   _u_xlat0.x at this point is mainTexRGBcomponentSource.x 
                //   (which is mainTexSample.a if _RGBRGBA_ON, or mainTexSample.r if _RGBRGBA_OFF)
                float mainTexAlphaSourceComponent;
                #if _RGBRGBA_ON
                    mainTexAlphaSourceComponent = mainTexSample.a;
                #else
                    // This is unusual, typically it would be mainTexSample.a not .r for alpha
                    // But following the decompiled logic:
                    mainTexAlphaSourceComponent = mainTexSample.r; 
                #endif
                finalAlpha *= mainTexAlphaSourceComponent;
                
                // (_u_xlat0.x = (_u_xlat0.x * __Color.w));
                finalAlpha *= _Color.a;
                
                // (_u_xlat0.x = (_u_xlat16_1 * _u_xlat0.x)); // _u_xlat16_1 is maskValue
                finalAlpha *= maskValue;
                
                // (_u_xlat2.w = (_u_xlat0.x * __Multiply));
                finalAlpha *= _Multiply;
                
                // (_u_xlat2.w = clamp(_u_xlat2.w, 0.0, 1.0));
                finalAlpha = saturate(finalAlpha);

                // (out_SV_Target0 = _u_xlat2);
                return float4(combinedRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}