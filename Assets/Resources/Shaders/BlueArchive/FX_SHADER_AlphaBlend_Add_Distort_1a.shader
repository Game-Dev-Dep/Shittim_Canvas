Shader "DSFX/FX_SHADER_AlphaBlend_Add_Distort_1a"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _Multiply("Multiply", Float) = 1
        _Tex_Main("Tex_Main", 2D) = "white" {}
        [KeywordEnum(RGB,RGBA)]_RGBRGBA("RGB>RGBA", Float) = 0
        [KeywordEnum(Off,Use)]_Main_Distort_Use("Main_Distort_Use", Float) = 0
        _Main_Speed_X("Main_Speed_X", Float) = 0
        _Main_Speed_Y("Main_Speed_Y", Float) = 0
        _Tex_Mask("Tex_Mask", 2D) = "white" {}
        [KeywordEnum(Off,Use)]_Mask_Distort_Use("Mask_Distort_Use", Float) = 0
        _Tex_Mask_Speed_X("Tex_Mask_Speed_X", Float) = 0
        _Tex_Mask_Speed_Y("Tex_Mask_Speed_Y", Float) = 0
        _Tex_Distort("Tex_Distort", 2D) = "white" {}
        _Dis_Speed_X("Dis_Speed_X", Float) = 0
        _Dis_Speed_Y("Dis_Speed_Y", Float) = 0
        _Distortion_Power_X("Distortion_Power_X", Float) = 0
        _Distortion_Power_Y("Distortion_Power_Y", Float) = 0
        [ASEnd]_Alphaclip_Value("Alphaclip_Value", Float) = 0
        [HideInInspector]_CustomData_MainTiling_CD1XYXY("CustomData_MainTiling_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MainTiling_CD2XYXY("CustomData_MainTiling_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MainOffset_CD1XYXY("CustomData_MainOffset_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MainOffset_CD2XYXY("CustomData_MainOffset_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MaskTiling_CD1XYXY("CustomData_MaskTiling_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MaskTiling_CD2XYXY("CustomData_MaskTiling_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MaskOffset_CD1XYXY("CustomData_MaskOffset_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_MaskOffset_CD2XYXY("CustomData_MaskOffset_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_DistortTiling_CD1XYXY("CustomData_DistortTiling_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_DistortTiling_CD2XYXY("CustomData_DistortTiling_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_DistortOffset_CD1XYXY("CustomData_DistortOffset_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_DistortOffset_CD2XYXY("CustomData_DistortOffset_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        [HideInInspector]_CustomData_DistortPower_CD2XYXY("CustomData_DistortPower_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        
        [Enum(UnityEngine.Rendering.BlendMode)][HideInInspector]_SrcBlend("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)][HideInInspector]_DstBlend("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.BlendMode)][HideInInspector]_SrcBlendAlpha("Src Blend Alpha", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)][HideInInspector]_DstBlendAlpha("Dst Blend Alpha", Float) = 10
        [Toggle(_ALPHAPREMULTIPLY_ON)][HideInInspector]_AlphaPremultiplyOn("Alpha Premultiply On", Float) = 0
        [Enum(Off,0,On,1)][HideInInspector]_ZWrite("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)][HideInInspector]_ZTest("Z Test", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)][HideInInspector]_Cull("Cull", Float) = 2
        [HideInInspector]_ZOffsetFactor("Z Offset Factor", Range(-1, 1)) = 0
        [HideInInspector]_ZOffsetUnits("Z Offset Units", Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        Blend One One
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull [_Cull]
        Offset [_ZOffsetFactor], [_ZOffsetUnits]
        
        Pass
        {
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHAPREMULTIPLY_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _Tex_Distort_ST;
            float4 _Tex_Main_ST;
            float4 _Tex_Mask_ST;
            float4 _CustomData_MainTiling_CD1XYXY;
            float4 _CustomData_MainOffset_CD2XYXY;
            float4 _CustomData_MainOffset_CD1XYXY;
            float4 _CustomData_MainTiling_CD2XYXY;
            float4 _CustomData_DistortOffset_CD2XYXY;
            float4 _CustomData_DistortTiling_CD2XYXY;
            float4 _CustomData_DistortOffset_CD1XYXY;
            float4 _CustomData_DistortTiling_CD1XYXY;
            float4 _CustomData_MaskTiling_CD1XYXY;
            float4 _CustomData_DistortPower_CD2XYXY;
            float4 _CustomData_MaskTiling_CD2XYXY;
            float4 _CustomData_MaskOffset_CD1XYXY;
            float4 _CustomData_MaskOffset_CD2XYXY;
            float _Tex_Mask_Speed_X;
            float _Tex_Mask_Speed_Y;
            float _Main_Speed_X;
            float _Dis_Speed_Y;
            float _Dis_Speed_X;
            float _Distortion_Power_Y;
            float _Distortion_Power_X;
            float _Main_Speed_Y;
            float _Multiply;
            float _Alphaclip_Value;
            CBUFFER_END
            
            TEXTURE2D(_Tex_Distort);
            SAMPLER(sampler_Tex_Distort);
            TEXTURE2D(_Tex_Main);
            SAMPLER(sampler_Tex_Main);
            TEXTURE2D(_Tex_Mask);
            SAMPLER(sampler_Tex_Mask);
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord0 : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord0 : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float2 vec2_ctor(float x0, float x1)
            {
                return float2(x0, x1);
            }
            
            float3 vec3_ctor(float x0)
            {
                return float3(x0, x0, x0);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord0 = input.texcoord0;
                output.texcoord1 = input.texcoord1;
                output.texcoord2 = input.texcoord2;
                output.color = input.color;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float4 _vs_TEXCOORD3 = input.texcoord0;
                float4 _vs_TEXCOORD4 = input.texcoord1;
                float4 _vs_TEXCOORD5 = input.texcoord2;
                float4 _vs_COLOR0 = input.color;
                
                float4 _u_xlat0;
                float4 _u_xlat16_0;
                float4 _u_xlat1;
                bool _u_xlatb1;
                float4 _u_xlat2;
                float2 _u_xlat4;
                float _u_xlat16_4;
                float2 _u_xlat6;
                float2 _u_xlat16_6;
                
                _u_xlat0 = (_vs_TEXCOORD4 * _CustomData_DistortPower_CD2XYXY);
                _u_xlat0.xy = (_u_xlat0.zw + _u_xlat0.xy);
                _u_xlat0.xy = (_u_xlat0.xy + vec2_ctor(_Distortion_Power_X, _Distortion_Power_Y));
                
                _u_xlat6.xy = ((_vs_TEXCOORD3.xy * _Tex_Distort_ST.xy) + _Tex_Distort_ST.zw);
                
                _u_xlat1.xw = (_Time.yy * vec2_ctor(_Dis_Speed_X, _Main_Speed_Y));
                _u_xlat1.yz = (_Time.yy * vec2_ctor(_Dis_Speed_Y, _Main_Speed_X));
                _u_xlat1.xy = frac(_u_xlat1.xy);
                
                _u_xlat6.xy = (_u_xlat6.xy + _u_xlat1.xy);
                _u_xlat6.xy = (_u_xlat6.xy + _vs_TEXCOORD3.zw);
                
                _u_xlat16_6.xy = SAMPLE_TEXTURE2D(_Tex_Distort, sampler_Tex_Distort, _u_xlat6.xy).xy;
                
                _u_xlat1.xy = ((_vs_TEXCOORD3.xy * _Tex_Main_ST.xy) + _Tex_Main_ST.zw);
                _u_xlat1.xy = (_u_xlat1.xy + _u_xlat1.zw);
                _u_xlat0.xy = ((_u_xlat0.xy * _u_xlat16_6.xy) + _u_xlat1.xy);
                _u_xlat0.xy = (_u_xlat0.xy + _vs_TEXCOORD5.xy);
                
                _u_xlat16_0 = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, _u_xlat0.xy);
                _u_xlat1.x = (_u_xlat16_0.x * _vs_COLOR0.w);
                _u_xlat1.x = (_u_xlat1.x * _Color.w);
                
                _u_xlat4.xy = ((_vs_TEXCOORD3.xy * _Tex_Mask_ST.xy) + _Tex_Mask_ST.zw);
                _u_xlat4.xy = ((vec2_ctor(_Tex_Mask_Speed_X, _Tex_Mask_Speed_Y) * _Time.yy) + _u_xlat4.xy);
                _u_xlat4.xy = (_u_xlat4.xy + _vs_TEXCOORD5.zw);
                _u_xlat16_4 = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, _u_xlat4.xy).x;
                
                _u_xlat2.w = (_u_xlat16_4 * _u_xlat1.x);
                _u_xlat2.w = clamp(_u_xlat2.w, 0.0, 1.0);
                _u_xlat1.x = (_u_xlat2.w + (-_Alphaclip_Value));
                _u_xlatb1 = (_u_xlat1.x < 0.0);
                
                if(_u_xlatb1)
                {
                    discard;
                }
                
                _u_xlat0.xyz = (_u_xlat16_0.xyz * _Color.xyz);
                _u_xlat0.xyz = (vec3_ctor(_u_xlat16_4) * _u_xlat0.xyz);
                _u_xlat0.xyz = (_u_xlat0.xyz * vec3_ctor(_Multiply));
                _u_xlat0.xyz = (_u_xlat16_0.www * _u_xlat0.xyz);
                _u_xlat2.xyz = (_u_xlat0.xyz * _vs_COLOR0.xyz);
                
                #ifdef _ALPHAPREMULTIPLY_ON
                _u_xlat2.rgb *= _u_xlat2.a;
                #endif
                
                return _u_xlat2;
            }
            ENDHLSL
        }
    }
    CustomEditor "ASEMaterialInspector"
}
