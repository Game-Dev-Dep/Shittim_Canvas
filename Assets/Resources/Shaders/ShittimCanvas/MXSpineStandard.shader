Shader "Spine/Skeleton MX Standard"
{
    Properties
    {
        [NoScaleOffset]_MainTex("Main Tex", 2D) = "black" {}
        
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend Alpha", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend Alpha", Float) = 10
        
        [Enum(Off,0,On,1)] _ZWrite("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            Lighting Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            VertexOutput vert(VertexInput IN)
            {
                VertexOutput OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(VertexOutput IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                
                //half4 texColor = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uv, _GlobalMipBias);
                half4 texColor = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uv, _GlobalMipBias.x);

                half3 premultipliedColor = texColor.rgb * texColor.a;
                half4 finalColor;
                finalColor.rgb = premultipliedColor * IN.color.rgb;
                finalColor.a = texColor.a * IN.color.a;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/Unlit"
}
