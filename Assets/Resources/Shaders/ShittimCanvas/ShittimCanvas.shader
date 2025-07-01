Shader "Hidden/ShittimCanvas"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _ShittimCanvasTex ("ShittimCanvas Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.05
        _Position ("Position", Vector) = (0.9, 0.1, 0, 0)
        _Scale ("Scale", Float) = 0.2
        _BlendMode ("Blend Mode", Float) = 3
        _Tiling ("Enable Tiling", Float) = 0
        _TilingCount ("Tiling Count", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Name "ShittimCanvas"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShittimCanvasTex);
            SAMPLER(sampler_ShittimCanvasTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShittimCanvasTex_ST;
                float _Opacity;
                float2 _Position;
                float _Scale;
                float _BlendMode;
                float _Tiling;
                float _TilingCount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // ���ģʽ����
            float3 BlendOverlay(float3 base, float3 blend)
            {
                float3 result;
                result.r = base.r < 0.5 ? (2.0 * base.r * blend.r) : (1.0 - 2.0 * (1.0 - base.r) * (1.0 - blend.r));
                result.g = base.g < 0.5 ? (2.0 * base.g * blend.g) : (1.0 - 2.0 * (1.0 - base.g) * (1.0 - blend.g));
                result.b = base.b < 0.5 ? (2.0 * base.b * blend.b) : (1.0 - 2.0 * (1.0 - base.b) * (1.0 - blend.b));
                return result;
            }

            float3 BlendScreen(float3 base, float3 blend)
            {
                return 1.0 - (1.0 - base) * (1.0 - blend);
            }

            float3 BlendMultiply(float3 base, float3 blend)
            {
                return base * blend;
            }

            float3 ApplyBlendMode(float3 base, float3 blend, float mode)
            {
                if (mode < 0.5) // Alpha
                    return base;
                else if (mode < 1.5) // Additive
                    return base + blend;
                else if (mode < 2.5) // Multiply
                    return BlendMultiply(base, blend);
                else if (mode < 3.5) // Overlay
                    return BlendOverlay(base, blend);
                else // Screen
                    return BlendScreen(base, blend);
            }

            float4 frag(Varyings input) : SV_Target
            {
                // ����������
                float4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                float2 ShittimCanvasUV;
                float ShittimCanvasAlpha = 0.0;
                
                if (_Tiling > 0.5)
                {
                    // ƽ��ģʽ
                    float2 tiledUV = input.uv * _TilingCount;
                    float2 cellUV = frac(tiledUV);
                    
                    // ��ÿ���������ķ���ˮӡ
                    float2 cellCenter = float2(0.5, 0.5);
                    float2 offset = cellUV - cellCenter;
                    
                    // ����ˮӡUV
                    ShittimCanvasUV = (offset / _Scale) + 0.5;
                    
                    // ����Ƿ���ˮӡ��Χ��
                    if (ShittimCanvasUV.x >= 0.0 && ShittimCanvasUV.x <= 1.0 && 
                        ShittimCanvasUV.y >= 0.0 && ShittimCanvasUV.y <= 1.0)
                    {
                        float4 ShittimCanvasColor = SAMPLE_TEXTURE2D(_ShittimCanvasTex, sampler_ShittimCanvasTex, ShittimCanvasUV);
                        ShittimCanvasAlpha = ShittimCanvasColor.a;
                    }
                }
                else
                {
                    // ����ˮӡģʽ
                    float2 screenPos = input.uv;
                    float2 ShittimCanvasCenter = _Position;
                    
                    // ���������ˮӡ���ĵ�ƫ��
                    float2 offset = screenPos - ShittimCanvasCenter;
                    
                    // Ӧ�����Ų�ת��������UV�ռ�
                    ShittimCanvasUV = (offset / _Scale) + 0.5;
                    
                    // ����Ƿ���ˮӡ��Χ��
                    if (ShittimCanvasUV.x >= 0.0 && ShittimCanvasUV.x <= 1.0 && 
                        ShittimCanvasUV.y >= 0.0 && ShittimCanvasUV.y <= 1.0)
                    {
                        float4 ShittimCanvasColor = SAMPLE_TEXTURE2D(_ShittimCanvasTex, sampler_ShittimCanvasTex, ShittimCanvasUV);
                        ShittimCanvasAlpha = ShittimCanvasColor.a;
                    }
                }
                
                if (ShittimCanvasAlpha > 0.01)
                {
                    float4 ShittimCanvasColor = SAMPLE_TEXTURE2D(_ShittimCanvasTex, sampler_ShittimCanvasTex, ShittimCanvasUV);
                    
                    // Ӧ�÷ǳ��͵�͸���ȣ�ʹˮӡ�������ɼ�
                    float finalOpacity = _Opacity * ShittimCanvasAlpha;
                    
                    // Ӧ�û��ģʽ
                    float3 blendedColor = ApplyBlendMode(mainColor.rgb, ShittimCanvasColor.rgb, _BlendMode);
                    
                    // ʹ�ü���͸���Ȼ��
                    mainColor.rgb = lerp(mainColor.rgb, blendedColor, finalOpacity);
                }
                
                return mainColor;
            }
            ENDHLSL
        }
    }
}