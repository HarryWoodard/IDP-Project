Shader "Custom/SpriteSolidColorURP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FlashColor ("Flash Color", Color) = (0.878, 0.973, 0.812, 1)
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Core URP shader library
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textures and Samplers are declared via macros in URP
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Variables must be inside a CBUFFER to support SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                float4 _FlashColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                // Converts Object Space to Clip Space using the URP helper function
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                // Samples texture using modern URP macro
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // Keeps the sprite's shape (alpha) and vertex color alpha, paints with Flash Color
                return float4(_FlashColor.rgb, tex.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
