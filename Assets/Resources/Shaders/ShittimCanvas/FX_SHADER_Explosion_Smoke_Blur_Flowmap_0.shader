Shader "DSFX/FX_SHADER_Explosion_Smoke_Blur_Flowmap_0"
{
    Properties
    {
        [HDR]_RColor("[R] Color", Color) = (1,1,1,1)
        _Mutiply("Multiply", Float) = 1
        _MainTexRGBA("Main Texture (RGBA)", 2D) = "white" {}
        [HDR]_GColor("[G] Color", Color) = (1,0.2989459,0,0)
        [Toggle]_Color_Custom_Data_Use("Use Color Custom Data", Float) = 1
        [Toggle]_GCustom_Data_Use("[G] Custom Data", Float) = 1
        _GColor_Sharpness("[G] Sharpness", Float) = 20
        _GDisappear_Offset("[G] Disappear Offset", Range(0,1)) = 0
        [Toggle]_BCustom_Data_Use("[B] Custom Data", Float) = 1
        _BStep_Glow_Value("[B] Glow Intensity", Float) = 20
        _BDisappear_Offset("[B] Disappear Offset", Range(0,1)) = 0
        _Shadow_Color_Multiply("Shadow Multiply", Range(0,1)) = 0.5
        [Toggle]_Shadow_Value_Data_Use("Shadow Data", Float) = 0
        _Shadow_Value("Shadow Value", Range(-0.3,0)) = 0
        _Shadow_Glow_Value("Shadow Glow", Float) = 20
        _FlowTexRGB("Flow Texture (RGB)", 2D) = "white" {}
        [Toggle]_Flowspeed_Data_Use("Flow Speed Data", Float) = 0
        _FlowSpeed("Flow Speed", Float) = 1
        [Toggle]_ZWrite_Mode("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest_Mode("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)]_Cull_Mode("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+200"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha 
            ZTest [_ZTest_Mode]
            ZWrite [_ZWrite_Mode]
            Cull [_Cull_Mode]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _COLOR_CUSTOM_DATA_USE_ON
            #pragma shader_feature_local _G_CUSTOM_DATA_USE_ON
            #pragma shader_feature_local _B_CUSTOM_DATA_USE_ON
            #pragma shader_feature_local _SHADOW_VALUE_DATA_USE_ON
            #pragma shader_feature_local _FLOWSPEED_DATA_USE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl" // ÐÂÔö

            


            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                float4 customData1 : TEXCOORD1; // x:Shadow, y:FlowSpeed, z:G Offset, w:B Offset
                float4 customData2 : TEXCOORD2; // RGB: GColor
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 customData : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTexRGBA); SAMPLER(sampler_MainTexRGBA);
            TEXTURE2D(_FlowTexRGB); SAMPLER(sampler_FlowTexRGB);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTexRGBA_ST;
                float4 _FlowTexRGB_ST;
                float4 _RColor;
                float4 _GColor;
                float _Mutiply;
                float _GColor_Sharpness;
                float _GDisappear_Offset;
                float _BStep_Glow_Value;
                float _BDisappear_Offset;
                float _Shadow_Color_Multiply;
                float _Shadow_Value;
                float _Shadow_Glow_Value;
                float _FlowSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv.xy = TRANSFORM_TEX(IN.uv.xy, _MainTexRGBA);
                OUT.uv.zw = TRANSFORM_TEX(IN.uv.xy, _FlowTexRGB);
                OUT.customData = IN.customData1;
                OUT.color = IN.color * float4(_RColor.rgb, 1.0);
                OUT.color.a *= _RColor.a;

                #if _G_CUSTOM_DATA_USE_ON
                    OUT.customData.z = IN.customData1.z;
                #endif

                #if _B_CUSTOM_DATA_USE_ON
                    OUT.customData.w = IN.customData1.w;
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Flow texture calculation
                float2 flowVector = SAMPLE_TEXTURE2D(_FlowTexRGB, sampler_FlowTexRGB, IN.uv.zw).xy * 2.0 - 1.0;
                
                #if _FLOWSPEED_DATA_USE_ON
                    float flowSpeed = IN.customData.y;
                #else
                    float flowSpeed = _FlowSpeed;
                #endif
                flowVector *= flowSpeed;

                // Time-based flow animation
                float phase = frac(_Time.y * 0.5);
                float2 uvOffset1 = IN.uv.xy - flowVector * phase;
                float2 uvOffset2 = IN.uv.xy - flowVector * (phase + 0.5);

                // Blend two flow phases
                half4 tex1 = SAMPLE_TEXTURE2D(_MainTexRGBA, sampler_MainTexRGBA, uvOffset1);
                half4 tex2 = SAMPLE_TEXTURE2D(_MainTexRGBA, sampler_MainTexRGBA, uvOffset2);
                half4 mainTex = lerp(tex1, tex2, abs(phase * 2.0 - 1.0));

                // Shadow calculation
                #if _SHADOW_VALUE_DATA_USE_ON
                    float shadowValue = IN.customData.x;
                #else
                    float shadowValue = _Shadow_Value;
                #endif

                half shadow = saturate((mainTex.z + shadowValue) * _Shadow_Glow_Value);
                half3 shadowColor = lerp(mainTex.r * _Shadow_Color_Multiply, mainTex.r, shadow);

                //return half4(shadowColor, 1);

                // G Channel processing
                #if _G_CUSTOM_DATA_USE_ON
                    float gOffset = IN.customData.z;
                #else
                    float gOffset = _GDisappear_Offset;
                #endif

                half gMask = saturate((mainTex.g - gOffset) * _GColor_Sharpness);
                half3 gColor = lerp(shadowColor * _GColor.rgb, _GColor.rgb, gMask);
                gColor = (0.15, 0.15, 0.15);

                //return half4(gColor, 1);

                // B Channel processing  
                #if _B_CUSTOM_DATA_USE_ON
                    float bOffset = IN.customData.w;
                #else
                    float bOffset = _BDisappear_Offset;
                #endif

                half bAlpha = saturate((mainTex.b - bOffset) * _BStep_Glow_Value) * mainTex.a;

                // Final color composition
                half3 finalColor = gColor * IN.color.rgb * _Mutiply;/*gColor * IN.color.rgb * _Mutiply;*/
                half alpha = bAlpha * IN.color.a * _Mutiply;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Lit"
    CustomEditor "ASEMaterialInspector"
}