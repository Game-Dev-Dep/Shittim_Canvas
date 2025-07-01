Shader "DSFX/FX_SHADER_AlphaBlend_Add"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1,1,1,1)
        _Multiply("Multiply", Float) = 1
        _Texture("Texture", 2D) = "white" {}
        [Toggle] _RGBRGBA("RGB>RGBA", Float) = 0
        [Toggle] _MainTexture_No("Main Texture No", Float) = 0
        [Toggle] _CustomData_Offset_Use("Custom Data Offset Use", Float) = 0
        [Toggle] _ZWriteMode("ZWrite Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest Mode", Float) = 4
        _ZOffsetFactor("ZOffset Factor", Float) = 0
        _ZOffsetUnits("ZOffset Units", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            
            Blend SrcAlpha One
            Cull [_CullMode]
            ZWrite [_ZWriteMode]
            ZTest [_ZTestMode]
            Offset [_ZOffsetFactor], [_ZOffsetUnits]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord : TEXCOORD0;
                float4 color : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Texture_ST;
                float _Multiply;
                float _RGBRGBA;
                float _MainTexture_No;
                float _CustomData_Offset_Use;
            CBUFFER_END

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                output.color = input.color;
                return output;
            }

            float3 vec3_ctor(float x0) { return float3(x0, x0, x0); }
            float4 vec4_ctor(float x0) { return float4(x0, x0, x0, x0); }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord.xy * _Texture_ST.xy + _Texture_ST.zw;
                float2 customUV = uv + input.texcoord.zw;
                
                // 选择使用原始UV还是带偏移的UV
                float2 finalUV = _CustomData_Offset_Use > 0 ? customUV : uv;
                
                // 采样纹理
                float4 texSample = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, finalUV);
                
                // 选择使用RGB通道还是Alpha通道
                float texValue = _RGBRGBA > 0 ? texSample.w : texSample.x;
                
                // 计算基础颜色
                float3 baseColor = _Color.rgb * vec3_ctor(_Multiply);
                baseColor *= vec3_ctor(texValue);
                
                // 计算透明度
                float alpha = texValue * input.color.w * _Color.w * _Multiply;
                alpha = saturate(alpha);
                
                // 选择纹理颜色
                float3 texColor = _MainTexture_No > 0 ? float3(1,1,1) : texSample.rgb;
                
                // 最终颜色计算
                float3 finalColor = texColor * input.color.rgb * baseColor;
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}