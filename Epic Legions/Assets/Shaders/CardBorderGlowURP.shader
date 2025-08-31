Shader "Hemera/CardBorderGlowURP"
{
    Properties
    {
        // Dummy para evitar errores en UI.Image / RawImage
        _MainTex ("UI MainTex (unused)", 2D) = "white" {}

        _MaskTex    ("Border Mask", 2D) = "white" {}
        _NoiseTex   ("Flow Noise", 2D) = "gray" {}
        _RampTex    ("Glow Ramp (U)", 2D) = "white" {}

        _GlowColor    ("Glow Color", Color) = (0, 0.5, 1, 1) // Azul por defecto
        _GlowIntensity ("Glow Intensity", Range(0,10)) = 3
        _DistortAmount ("Distort Amount", Range(0,0.2)) = 0.05
        _PulseSpeed    ("Pulse Speed", Range(0,10)) = 2.5

        _Scroll1      ("Scroll1 (U,V,Scale,0)", Vector) = (0.35, 0.0, 1.0, 0.0)
        _Scroll2      ("Scroll2 (U,V,Scale,0)", Vector) = (0.0, 0.25, 2.0, 0.0)

        _Expand       ("Mask Expand (0-0.2)", Range(0,0.2)) = 0.03
        _Alpha        ("Global Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Dummy MainTex (nunca lo usamos pero Unity UI lo busca)
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_RampTex);  SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; // Dummy para RawImage/UI
                float4 _MaskTex_ST;
                float4 _NoiseTex_ST;
                float4 _RampTex_ST;

                float4 _GlowColor;
                float _GlowIntensity;
                float _DistortAmount;
                float _PulseSpeed;
                float4 _Scroll1; // xy = dir, z = scale
                float4 _Scroll2; // xy = dir, z = scale
                float _Expand;
                float _Alpha;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float2 expand_uv(float2 uv, float expandAmount)
            {
                return (uv - 0.5) * (1.0 - expandAmount) + 0.5;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // Flow noise (two layers)
                float t = _Time.y;
                float2 uv1 = uv * _Scroll1.z + _Scroll1.xy * t;
                float2 uv2 = uv * _Scroll2.z + _Scroll2.xy * (t * 1.2);

                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv1).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv2).g;
                float flow = saturate(n1 * 0.6 + n2 * 0.5);

                // Distort mask
                float2 uvDist = uv + (flow - 0.5) * _DistortAmount;

                // Mask expand
                float2 uvMask = expand_uv(uvDist, _Expand);
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask).r;

                // Ramp
                float ramp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(flow, 0.0)).r;

                // Pulse
                float pulse = 0.5 + 0.4 * sin(t * _PulseSpeed);

                // Emission color
                float3 emission = ramp * _GlowColor.rgb * mask * _GlowIntensity * pulse;

                // Solo borde visible
                float alpha = mask * _Alpha;

                return half4(emission, alpha);
            }
            ENDHLSL
        }
    }
}
