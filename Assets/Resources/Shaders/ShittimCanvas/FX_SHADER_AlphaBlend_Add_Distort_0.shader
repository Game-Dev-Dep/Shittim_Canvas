Shader "DSFX/FX_SHADER_AlphaBlend_Add_Distort_0" {
    Properties {
        [HDR] _Color ("Color", Vector) = (1,1,1,1)
        _Multiply ("Multiply", Float) = 1
        _Tex_Main ("Tex_Main", 2D) = "white" {}
        [Toggle] _RGBRGBA ("RGB>RGBA", Float) = 0
        [Toggle] _Main_Distortion_Use ("Main_Distortion_Use", Float) = 0
        _Main_Speed_X ("Main_Speed_X", Float) = 0
        _Main_Speed_Y ("Main_Speed_Y", Float) = 0
        [Toggle] _Custom_Data_MainMask_Offset_Use ("Custom_Data_Main/Mask_Offset_Use", Float) = 0
        
        _Tex_Mask ("Tex_Mask", 2D) = "white" {}
        [Toggle] _Mask_Distortion_Use ("Mask_Distortion_Use", Float) = 0
        _Mask_Speed_X ("Mask_Speed_X", Float) = 0
        _Mask_Speed_Y ("Mask_Speed_Y", Float) = 0
        
        _Tex_Distort ("Tex_Distort", 2D) = "white" {}
        _Dis_Speed_X ("Dis_Speed_X", Float) = 0
        _Dis_Speed_Y ("Dis_Speed_Y", Float) = 0
        _Distortion_Power_X ("Distortion_Power_X", Float) = 0
        _Distortion_Power_Y ("Distortion_Power_Y", Float) = 0
        [Toggle] _Custom_Data_Distort_Power_Use ("Custom_Data_Distort_Power_Use", Float) = 0
        
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest_Mode ("ZTest Mode", Float) = 4 // Default: LessEqual
        [Toggle] _ZWrite_Mode ("ZWrite Mode (0=Off, 1=On)", Float) = 0 // Shader Pass will use ZWrite Off/On based on this. For simplicity, this shader uses ZWrite Off. Use variants for dynamic control.
        [Enum(UnityEngine.Rendering.CullMode)] _Cull_Mode ("Cull Mode", Float) = 2 // Default: Back
        _ZOffsetFactor ("ZOffsetFactor", Float) = 0
        _ZOffsetUnits ("ZOffsetUnits", Float) = 0
    }
    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "DisableBatching"="True" // Good for effects with custom vertex data or screen-space effects
            "ForceNoShadowCasting"="True"
        }

        Pass {

            Blend SrcAlpha One // Alpha Blend (color * srcAlpha) then Additive (result + dest)
            ZWrite [_ZWrite_Mode] // Use ZWrite Off for typical transparent effects. [Toggle] property can't directly drive this without shader_feature.
                                // For this restoration, let's assume if _ZWrite_Mode is 0, ZWrite is Off. Otherwise On.
                                // Unity might not parse [_ZWrite_Mode] directly if it's a float.
                                // Explicitly: ZWrite Off (typical for such effects)
                                // If _ZWrite_Mode is non-zero, it would be ZWrite On.
                                // For simplicity, this example will use ZWrite Off.
                                // To make it property-driven, you'd use #pragma shader_feature and two ZWrite states.
            ZWrite Off // Defaulting to Off. Change to On or use keywords if ZWrite is needed.
            Cull [_Cull_Mode]
            ZTest [_ZTest_Mode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma warning( disable: 3556 3571 ) // Keep original warnings disabled

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl" // For _Time

            // Helper functions from original code
            bool bool_ctor(bool x0) { return bool(x0); }
            float2 vec2_ctor(float x0, float x1) { return float2(x0, x1); }
            float3 vec3_ctor(float x0, float x1, float x2) { return float3(x0, x1, x2); }
            float3 vec3_ctor(float3 x0) { return float3(x0); }
            float4 vec4_ctor(float x0) { return float4(x0, x0, x0, x0); }
            float4 vec4_ctor(float x0, float x1, float x2, float x3) { return float4(x0, x1, x2, x3); }

            // Texture declarations for URP
            TEXTURE2D(_Tex_Distort);    SAMPLER(sampler_Tex_Distort);
            TEXTURE2D(_Tex_Main);       SAMPLER(sampler_Tex_Main);
            TEXTURE2D(_Tex_Mask);       SAMPLER(sampler_Tex_Mask);

            CBUFFER_START(UnityPerMaterial)
                // Properties from ShaderLab, names match properties block
                float4 _Tex_Mask_ST;
                float4 _Color;
                float4 _Tex_Main_ST;
                float4 _Tex_Distort_ST;
                // _ZWrite_Mode is handled by Pass state
                float _Mask_Distortion_Use;
                float _Mask_Speed_Y;
                float _Mask_Speed_X;
                float _Dis_Speed_Y;
                float _Dis_Speed_X;
                float _Multiply;
                float _Distortion_Power_Y;
                float _Custom_Data_Distort_Power_Use;
                float _Main_Distortion_Use;
                float _Main_Speed_Y;
                float _Main_Speed_X;
                float _Custom_Data_MainMask_Offset_Use;
                // _ZOffsetUnits, _ZTest_Mode, _Cull_Mode, _ZOffsetFactor are handled by Pass states
                float _Distortion_Power_X;
                float _RGBRGBA;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0; // For v2.xy (main UVs)
                float4 texcoord1    : TEXCOORD1; // For v0 (Custom_Data_MainMask_Offset)
                float4 texcoord2    : TEXCOORD2; // For v1 (Custom_Data_Distort_Power)
                float4 color        : COLOR;     // For v3 (Vertex Color)
            };

            // This struct matches the original PS_INPUT structure
            struct Varyings
            {
                float4 positionCS     : SV_Position; // Was dx_Position
                float4 clipPosForFrag : TEXCOORD4;     // Was gl_Position (not used in fragment logic)
                float4 v0             : TEXCOORD0;     // Data for _vs_TEXCOORD3
                float4 v1             : TEXCOORD1;     // Data for _vs_TEXCOORD4
                float4 v2             : TEXCOORD2;     // Data for _vs_TEXCOORD5 (v2.xy = main UVs)
                float4 v3             : TEXCOORD3;     // Data for _vs_COLOR0 (vertex color)
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.clipPosForFrag = output.positionCS; // Matching original PS_INPUT structure

                output.v0 = input.texcoord1;    // Pass through custom data
                output.v1 = input.texcoord2;    // Pass through custom data
                output.v2 = float4(input.uv.x, input.uv.y, 0, 0); // Main UVs in v2.xy
                output.v3 = input.color;        // Vertex color
                
                return output;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Local variables, originally static globals like _u_xlat0
                float2 _u_xlat0;
                float2 _u_xlat16_0;
                float4 _u_xlat1;
                bool _u_xlatb1;
                float2 _u_xlat2;
                float4 _u_xlat16_2;
                float3 _u_xlat3;
                float3 _u_xlat4;
                bool _u_xlatb4;
                float2 _u_xlat8;
                bool2 _u_xlatb8;
                float2 _u_xlat9;

                // Original fragment shader logic, with _vs_TEXCOORDX replaced by i.vX
                // and __PropertyName replaced by _PropertyName
                // and gl_texture2D replaced by SAMPLE_TEXTURE2D
                // and __Time replaced by _Time

                (_u_xlat0.xy = ((i.v2.xy * _Tex_Distort_ST.xy) + _Tex_Distort_ST.zw));
                (_u_xlat0.xy = ((_Time.yy * vec2_ctor(_Dis_Speed_X, _Dis_Speed_Y)) + _u_xlat0.xy));
                (_u_xlat16_0.xy = SAMPLE_TEXTURE2D(_Tex_Distort, sampler_Tex_Distort, _u_xlat0.xy).xy);
                (_u_xlatb8.xy = (float4(0.0, 0.0, 0.0, 0.0) != vec4_ctor(_Custom_Data_Distort_Power_Use, _Main_Distortion_Use, _Custom_Data_Distort_Power_Use, _Main_Distortion_Use)).xy);
                
                float2 s1614;
                if (_u_xlatb8.x) {
                    (s1614 = i.v1.xy); // _vs_TEXCOORD4.xy -> i.v1.xy
                } else {
                    (s1614 = float2(0.0, 0.0));
                }
                (_u_xlat1.xy = s1614);
                (_u_xlat2.x = (_u_xlat1.x + _Distortion_Power_X));
                (_u_xlat2.y = (_u_xlat1.y + _Distortion_Power_Y));
                (_u_xlat0.xy = (_u_xlat16_0.xy * _u_xlat2.xy));
                
                float2 s1615;
                if (_u_xlatb8.y) {
                    (s1615 = _u_xlat0.xy);
                } else {
                    (s1615 = float2(0.0, 0.0));
                }
                (_u_xlat8.xy = s1615);
                (_u_xlatb1 = !all(float4(0.0, 0.0, 0.0, 0.0) == vec4_ctor(_Custom_Data_MainMask_Offset_Use)));
                
                float4 s1616;
                if (bool_ctor(_u_xlatb1)) {
                    (s1616 = i.v0); // _vs_TEXCOORD3 -> i.v0
                } else {
                    (s1616 = float4(0.0, 0.0, 0.0, 0.0));
                }
                (_u_xlat1 = s1616);
                (_u_xlat1.xy = ((_Time.yy * vec2_ctor(_Main_Speed_X, _Main_Speed_Y)) + _u_xlat1.xy));
                (_u_xlat9.xy = ((_Time.yy * vec2_ctor(_Mask_Speed_X, _Mask_Speed_Y)) + _u_xlat1.zw));
                (_u_xlat8.xy = (_u_xlat8.xy + _u_xlat1.xy));
                (_u_xlat1.xy = ((i.v2.xy * _Tex_Main_ST.xy) + _Tex_Main_ST.zw)); // _vs_TEXCOORD5.xy -> i.v2.xy
                (_u_xlat8.xy = (_u_xlat8.xy + _u_xlat1.xy));
                (_u_xlat16_2 = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, _u_xlat8.xy));
                (_u_xlat3.xyz = (_u_xlat16_2.xyz * i.v3.xyz)); // _vs_COLOR0.xyz -> i.v3.xyz
                (_u_xlat3.xyz = (_u_xlat3.xyz * _Color.xyz));
                (_u_xlat8.xy = ((i.v2.xy * _Tex_Mask_ST.xy) + _Tex_Mask_ST.zw)); // _vs_TEXCOORD5.xy -> i.v2.xy
                (_u_xlat8.xy = (_u_xlat8.xy + _u_xlat9.xy));
                (_u_xlatb1 = !all(float4(0.0, 0.0, 0.0, 0.0) == vec4_ctor(_Mask_Distortion_Use)));
                
                float2 s1617;
                if (bool_ctor(_u_xlatb1)) {
                    (s1617 = _u_xlat0.xy);
                } else {
                    (s1617 = float2(0.0, 0.0));
                }
                (_u_xlat0.xy = s1617);
                (_u_xlat0.xy = (_u_xlat0.xy + _u_xlat8.xy));
                (_u_xlat16_0.x = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, _u_xlat0.xy).x);
                (_u_xlat4.xyz = (_u_xlat16_0.xxx * _u_xlat3.xyz));
                (_u_xlat4.xyz = (_u_xlat4.xyz * vec3_ctor(vec3_ctor(_Multiply, _Multiply, _Multiply))));
                (_u_xlat1.xyz = (_u_xlat16_2.www * _u_xlat4.xyz)); // Assuming _u_xlat16_2.www is main texture alpha. Could be .aaa if not RGBA
                (_u_xlatb4 = !all(float4(0.0, 0.0, 0.0, 0.0) == vec4_ctor(_RGBRGBA)));
                
                float s1618;
                if (_u_xlatb4) { // If _RGBRGBA is true (non-zero)
                    (s1618 = _u_xlat16_2.w); // Use Alpha from Main Texture
                } else {
                    (s1618 = _u_xlat16_2.x); // Use Red from Main Texture as Alpha
                }
                (_u_xlat4.x = s1618);
                (_u_xlat8.x = (i.v3.w * i.v3.w)); // _vs_COLOR0.w -> i.v3.w (vertex alpha squared)
                (_u_xlat4.x = (_u_xlat8.x * _u_xlat4.x));
                (_u_xlat4.x = (_u_xlat4.x * _Color.w)); // Tint Alpha
                (_u_xlat0.x = (_u_xlat16_0.x * _u_xlat4.x)); // Mask Alpha * (Tint Alpha * Vertex Alpha^2 * Tex Alpha (or R))
                (_u_xlat1.w = (_u_xlat0.x * _Multiply));
                (_u_xlat1.w = clamp(_u_xlat1.w, 0.0, 1.0));
                
                return _u_xlat1; // This was out_SV_Target0
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}