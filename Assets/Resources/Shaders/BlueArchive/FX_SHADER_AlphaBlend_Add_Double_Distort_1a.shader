Shader "DSFX/FX_SHADER_AlphaBlend_Add_Double_Distort_1a"
{
    // Properties
    // {
    //     [HDR] _Color("Color", Color) = (1,1,1,1)
    //     _Multiply("Multiply", Float) = 1
    //     _Tex_Main("Tex_Main", 2D) = "white" {}
    //     [KeywordEnum(RGB,RGBA)] _RGBRGBA("RGB>RGBA", Float) = 0
    //     _Main_Speed_X("Main_Speed_X", Float) = 0
    //     _Main_Speed_Y("Main_Speed_Y", Float) = 0
    //     [KeywordEnum(Off,R,G,RG)] _Tex_Main_Distort("Tex_Main_Distort", Float) = 0
    //     _Tex_Mask("Tex_Mask", 2D) = "white" {}
    //     _Mask_Speed_X("Mask_Speed_X", Float) = 0
    //     _Mask_Speed_Y("Mask_Speed_Y", Float) = 0
    //     [KeywordEnum(Off,R,G,RG)] _Tex_Mask_Distort("Tex_Mask_Distort", Float) = 0
    //     _Tex_DistortRG("Tex_Distort[R,G]", 2D) = "white" {}
    //     _RTilingxyOffsetzw("[R]Tiling[x,y]Offset[z,w]", Vector) = (1,1,0,0)
    //     _RDistort_Speed_X("[R]Distort_Speed_X", Float) = 0
    //     _RDistort_Speed_Y("[R]Distort_Speed_Y", Float) = 0
    //     _RDistort_Power_X("[R]Distort_Power_X", Float) = 0
    //     _RDistort_Power_Y("[R]Distort_Power_Y", Float) = 0
    //     _GTilingxyOffsetzw("[G]Tiling[x,y]Offset[z,w]", Vector) = (1,1,0,0)
    //     _GDistort_Speed_X("[G]Distort_Speed_X", Float) = 0
    //     _GDistort_Speed_Y("[G]Distort_Speed_Y", Float) = 0
    //     _GDistort_Power_X("[G]Distort_Power_X", Float) = 0
    //     _GDistort_Power_Y("[G]Distort_Power_Y", Float) = 0
    //     [ASEEnd] _Alphaclip_Value("Alphaclip_Value", Float) = 0
        
    //     [HideInInspector] _CustomData_MainTiling_CD1XYXY("CustomData_MainTiling_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MainTiling_CD2XYXY("CustomData_MainTiling_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MainOffset_CD1XYXY("CustomData_MainOffset_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MainOffset_CD2XYXY("CustomData_MainOffset_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MaskTiling_CD1XYXY("CustomData_MaskTiling_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MaskTiling_CD2XYXY("CustomData_MaskTiling_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MaskOffset_CD1XYXY("CustomData_MaskOffset_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_MaskOffset_CD2XYXY("CustomData_MaskOffset_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortTilingR_CD1XYXY("CustomData_DistortTiling[R]_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortTilingR_CD2XYXY("CustomData_DistortTiling[R]_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortOffsetR_CD1XYXY("CustomData_DistortOffset[R]_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortOffsetR_CD2XYXY("CustomData_DistortOffset[R]_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortPowerR_CD2XYXY("CustomData_DistortPower[R]_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortTilingG_CD1XYXY("CustomData_DistortTiling[G]_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortTilingG_CD2XYXY("CustomData_DistortTiling[G]_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortOffsetG_CD1XYXY("CustomData_DistortOffset[G]_CD1[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortOffsetG_CD2XYXY("CustomData_DistortOffset[G]_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
    //     [HideInInspector] _CustomData_DistortPowerG_CD2XYXY("CustomData_DistortPower[G]_CD2[X,Y][X,Y]", Vector) = (0,0,0,0)
        
    //     [Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _SrcBlend("Src Blend", Float) = 5
    //     [Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _DstBlend("Dst Blend", Float) = 10
    //     [Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _SrcBlendAlpha("Src Blend Alpha", Float) = 1
    //     [Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _DstBlendAlpha("Dst Blend Alpha", Float) = 10
    //     [Toggle(_ALPHAPREMULTIPLY_ON)] [HideInInspector] _AlphaPremultiplyOn("Alpha Premultiply On", Float) = 0
    //     [Enum(Off,0,On,1)] [HideInInspector] _ZWrite("Z Write", Float) = 0
    //     [Enum(UnityEngine.Rendering.CompareFunction)] [HideInInspector] _ZTest("Z Test", Float) = 4
    //     [Enum(UnityEngine.Rendering.CullMode)] [HideInInspector] _Cull("Cull", Float) = 2
    //     [HideInInspector] _ZOffsetFactor("Z Offset Factor", Range(-1, 1)) = 0
    //     [HideInInspector] _ZOffsetUnits("Z Offset Units", Range(-1, 1)) = 0
    // }
    
    // SubShader
    // {
    //     Tags
    //     {
    //         "RenderPipeline" = "UniversalPipeline"
    //         "RenderType" = "Transparent"
    //         "Queue" = "Transparent"
    //         "IgnoreProjector" = "True"
    //     }
        
    //     Pass
    //     {
    //         Name "ForwardLit"
    //         Tags { "LightMode" = "UniversalForward" }
            
    //         Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
    //         ZWrite [_ZWrite]
    //         ZTest [_ZTest]
    //         Cull [_Cull]
    //         Offset [_ZOffsetFactor], [_ZOffsetUnits]
            
    //         HLSLPROGRAM
    //         #pragma target 3.0
            
    //         -------------------------------------
    //         Universal Pipeline keywords
    //         #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
    //         #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
    //         #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
    //         #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
    //         #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
    //         --------------------------------------
    //         GPU Instancing
    //         #pragma multi_compile_instancing
    //         #pragma instancing_options renderinglayer
            
    //         #pragma vertex vert
    //         #pragma fragment frag
            
    //         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    //         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
    //         struct Attributes
    //         {
    //             float4 positionOS   : POSITION;
    //             float4 uv0          : TEXCOORD0;
    //             float4 uv1          : TEXCOORD1;
    //             float4 uv2          : TEXCOORD2;
    //             float4 uv3          : TEXCOORD3;
    //             float4 color        : COLOR;
    //             UNITY_VERTEX_INPUT_INSTANCE_ID
    //         };
            
    //         struct Varyings
    //         {
    //             float4 positionCS   : SV_POSITION;
    //             float4 v0           : TEXCOORD0;
    //             float4 v1           : TEXCOORD1;
    //             float4 v2           : TEXCOORD2;
    //             float4 v3           : TEXCOORD3;
    //             float4 v4           : TEXCOORD4;
    //             UNITY_VERTEX_INPUT_INSTANCE_ID
    //             UNITY_VERTEX_OUTPUT_STEREO
    //         };
            
    //         Material properties
    //         TEXTURE2D(_Tex_DistortRG);
    //         SAMPLER(sampler_Tex_DistortRG);
    //         TEXTURE2D(_Tex_Main);
    //         SAMPLER(sampler_Tex_Main);
    //         TEXTURE2D(_Tex_Mask);
    //         SAMPLER(sampler_Tex_Mask);
            
    //         CBUFFER_START(UnityPerMaterial)
    //             float4 _Color;
    //             float4 _CustomData_MaskOffset_CD1XYXY;
    //             float4 _CustomData_MaskTiling_CD2XYXY;
    //             float4 _Tex_Mask_ST;
    //             float4 _CustomData_MaskTiling_CD1XYXY;
    //             float4 _CustomData_MainOffset_CD2XYXY;
    //             float4 _CustomData_MainOffset_CD1XYXY;
    //             float4 _CustomData_MainTiling_CD2XYXY;
    //             float4 _CustomData_MainTiling_CD1XYXY;
    //             float4 _CustomData_DistortPowerG_CD2XYXY;
    //             float4 _CustomData_DistortOffsetG_CD2XYXY;
    //             float4 _CustomData_DistortTilingG_CD2XYXY;
    //             float4 _CustomData_DistortOffsetG_CD1XYXY;
    //             float4 _CustomData_MaskOffset_CD2XYXY;
    //             float4 _GTilingxyOffsetzw;
    //             float4 _CustomData_DistortTilingG_CD1XYXY;
    //             float4 _RTilingxyOffsetzw;
    //             float4 _CustomData_DistortTilingR_CD2XYXY;
    //             float4 _CustomData_DistortOffsetR_CD1XYXY;
    //             float4 _Tex_DistortRG_ST;
    //             float4 _CustomData_DistortTilingR_CD1XYXY;
    //             float4 _Tex_Main_ST;
    //             float4 _CustomData_DistortOffsetR_CD2XYXY;
    //             float4 _CustomData_DistortPowerR_CD2XYXY;
    //             float _Main_Speed_X;
    //             float _Main_Speed_Y;
    //             float _RDistort_Power_X;
    //             float _Mask_Speed_Y;
    //             float _Mask_Speed_X;
    //             float _GDistort_Speed_X;
    //             float _RDistort_Power_Y;
    //             float _RDistort_Speed_X;
    //             float _RDistort_Speed_Y;
    //             float _GDistort_Power_Y;
    //             float _GDistort_Power_X;
    //             float _GDistort_Speed_Y;
    //             float _Multiply;
    //             float _Alphaclip_Value;
    //         CBUFFER_END
            
    //         Varyings vert(Attributes input)
    //         {
    //             Varyings output = (Varyings)0;
    //             UNITY_SETUP_INSTANCE_ID(input);
    //             UNITY_TRANSFER_INSTANCE_ID(input, output);
    //             UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
    //             VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    //             output.positionCS = vertexInput.positionCS;
                
    //             Pass through UVs and vertex color
    //             output.v0 = input.uv0;
    //             output.v1 = input.uv1;
    //             output.v2 = input.uv2;
    //             output.v3 = input.color;
    //             output.v4 = input.uv3;
                
    //             return output;
    //         }
            
    //         float2 vec2_ctor(float x0)
    //         {
    //             return float2(x0, x0);
    //         }
            
    //         float2 vec2_ctor(float x0, float x1)
    //         {
    //             return float2(x0, x1);
    //         }
            
    //         float3 vec3_ctor(float x0)
    //         {
    //             return float3(x0, x0, x0);
    //         }
            
    //         half4 frag(Varyings input) : SV_Target
    //         {
    //             UNITY_SETUP_INSTANCE_ID(input);
    //             UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
    //             float4 _u_xlat0;
    //             float _u_xlat16_0;
    //             bool _u_xlatb0;
    //             float4 _u_xlat1;
    //             float4 _u_xlat16_1;
    //             float4 _u_xlat2;
    //             float3 _u_xlat3;
    //             float2 _u_xlat6;
    //             float _u_xlat16_6;
    //             float2 _u_xlat7;
                
    //             _u_xlat0.xz = (_Time.yy * vec2_ctor(_RDistort_Speed_X, _GDistort_Speed_X));
    //             _u_xlat0.yw = (_Time.yy * vec2_ctor(_RDistort_Speed_Y, _GDistort_Speed_Y));
    //             _u_xlat0 = frac(_u_xlat0);
                
    //             _u_xlat1.xy = ((input.v0.xy * _GTilingxyOffsetzw.xy) + _GTilingxyOffsetzw.zw);
    //             _u_xlat6.xy = (_u_xlat0.zw + _u_xlat1.xy);
    //             _u_xlat6.xy = (_u_xlat6.xy + input.v2.xy);
    //             _u_xlat16_6 = SAMPLE_TEXTURE2D(_Tex_DistortRG, sampler_Tex_DistortRG, _u_xlat6.xy).y;
                
    //             _u_xlat1 = (input.v1 * _CustomData_DistortPowerG_CD2XYXY);
    //             _u_xlat1.xy = (_u_xlat1.zw + _u_xlat1.xy);
    //             _u_xlat1.xy = (_u_xlat1.xy + vec2_ctor(_GDistort_Power_X, _GDistort_Power_Y));
                
    //             _u_xlat7.y = (_Time.y * _Mask_Speed_Y);
    //             _u_xlat7.x = (_Time.y * _Mask_Speed_X);
    //             _u_xlat6.xy = ((vec2_ctor(_u_xlat16_6) * _u_xlat1.xy) + _u_xlat7.xy);
    //             _u_xlat6.xy = (_u_xlat6.xy + input.v4.xy);
                
    //             _u_xlat1.xy = ((input.v0.xy * _Tex_Mask_ST.xy) + _Tex_Mask_ST.zw);
    //             _u_xlat6.xy = (_u_xlat6.xy + _u_xlat1.xy);
    //             _u_xlat16_6 = SAMPLE_TEXTURE2D(_Tex_Mask, sampler_Tex_Mask, _u_xlat6.xy).x;
                
    //             _u_xlat1.xy = ((input.v0.xy * _RTilingxyOffsetzw.xy) + _RTilingxyOffsetzw.zw);
    //             _u_xlat0.xy = (_u_xlat0.xy + _u_xlat1.xy);
    //             _u_xlat0.xy = (_u_xlat0.xy + input.v0.zw);
    //             _u_xlat16_0 = SAMPLE_TEXTURE2D(_Tex_DistortRG, sampler_Tex_DistortRG, _u_xlat0.xy).x;
                
    //             _u_xlat3.xz = ((input.v0.xy * _Tex_Main_ST.xy) + _Tex_Main_ST.zw);
    //             _u_xlat3.xz = ((vec2_ctor(_Main_Speed_X, _Main_Speed_Y) * _Time.yy) + _u_xlat3.xz);
                
    //             _u_xlat1 = (input.v1 * _CustomData_DistortPowerR_CD2XYXY);
    //             _u_xlat1.xy = (_u_xlat1.zw + _u_xlat1.xy);
    //             _u_xlat2.x = (_u_xlat1.x + _RDistort_Power_X);
    //             _u_xlat2.y = (_u_xlat1.y + _RDistort_Power_Y);
                
    //             _u_xlat0.xy = ((_u_xlat2.xy * vec2_ctor(_u_xlat16_0)) + _u_xlat3.xz);
    //             _u_xlat0.xy = (_u_xlat0.xy + input.v2.zw);
    //             _u_xlat16_1 = SAMPLE_TEXTURE2D(_Tex_Main, sampler_Tex_Main, _u_xlat0.xy);
                
    //             _u_xlat0.x = (input.v3.w * input.v3.w);
    //             _u_xlat0.x = (_u_xlat0.x * _u_xlat16_1.x);
    //             _u_xlat0.x = (_u_xlat0.x * _Color.w);
    //             _u_xlat0.x = (_u_xlat0.x * _Multiply);
                
    //             _u_xlat2.w = (_u_xlat16_6 * _u_xlat0.x);
    //             _u_xlat2.w = clamp(_u_xlat2.w, 0.0, 1.0);
    //             _u_xlat0.x = (_u_xlat2.w + (-_Alphaclip_Value));
    //             _u_xlatb0 = (_u_xlat0.x < 0.0);
                
    //             if(_u_xlatb0)
    //             {
    //                 discard;
    //             }
                
    //             _u_xlat0.xyw = (_u_xlat16_1.xyz * input.v3.xyz);
    //             _u_xlat0.xyw = (_u_xlat0.xyw * _Color.xyz);
    //             _u_xlat0.xyw = (_u_xlat0.xyw * vec3_ctor(_Multiply));
    //             _u_xlat0.xyw = (_u_xlat16_1.www * _u_xlat0.xyw);
    //             _u_xlat2.xyz = (vec3_ctor(_u_xlat16_6) * _u_xlat0.xyw);
                
    //             return _u_xlat2;
    //         }
    //         ENDHLSL
    //     }
        
    //     Shadow casting pass (if needed)
    //     Pass
    //     {
    //         Name "ShadowCaster"
    //         Tags{"LightMode" = "ShadowCaster"}
            
    //         ZWrite On
    //         ZTest LEqual
    //         Cull Off
            
    //         HLSLPROGRAM
    //         #pragma target 3.0
            
    //         #pragma vertex ShadowPassVertex
    //         #pragma fragment ShadowPassFragment
            
    //         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    //         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
    //         struct Attributes
    //         {
    //             float4 positionOS : POSITION;
    //             float3 normalOS : NORMAL;
    //             UNITY_VERTEX_INPUT_INSTANCE_ID
    //         };
            
    //         struct Varyings
    //         {
    //             float4 positionCS : SV_POSITION;
    //             UNITY_VERTEX_INPUT_INSTANCE_ID
    //         };
            
    //         Varyings ShadowPassVertex(Attributes input)
    //         {
    //             Varyings output;
    //             UNITY_SETUP_INSTANCE_ID(input);
                
    //             float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    //             float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    //             float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
                
    //             output.positionCS = positionCS;
    //             return output;
    //         }
            
    //         half4 ShadowPassFragment(Varyings input) : SV_TARGET
    //         {
    //             return 0;
    //         }
    //         ENDHLSL
    //     }
    // }
}
