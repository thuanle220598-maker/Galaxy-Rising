Shader "GalaxyRising/ProceduralFire"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _UseSpriteMask ("Use Sprite Mask", Range(0, 1)) = 0
        _SpriteContribution ("Sprite Contribution", Range(0, 1)) = 0
        _FlowDirection ("Flow Direction", Vector) = (0, 1, 0, 0)
        _OuterColor ("Outer Color", Color) = (0.055, 0.008, 0.16, 1)
        _MidColor ("Mid Color", Color) = (0.42, 0.025, 1, 1)
        _InnerColor ("Inner Color", Color) = (1, 0.08, 0.75, 1)
        _HotColor ("Hot Color", Color) = (1, 0.82, 1, 1)
        _Speed ("Flow Speed", Float) = 1.35
        _Phase ("Phase", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _Fade ("Fade", Range(0, 1)) = 1
        _PreviewTime ("Preview Time", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float _UseSpriteMask;
                float _SpriteContribution;
                float4 _FlowDirection;
                half4 _OuterColor;
                half4 _MidColor;
                half4 _InnerColor;
                half4 _HotColor;
                float _Speed;
                float _Phase;
                float _Intensity;
                float _Fade;
                float _PreviewTime;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                return lerp(
                    lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), local.x),
                    lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), local.x),
                    local.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = p * 2.03 + float2(17.17, 9.23);
                    amplitude *= 0.5;
                }
                return value;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 direction = _FlowDirection.xy;
                direction = dot(direction, direction) < 0.001
                    ? float2(0.0, 1.0)
                    : normalize(direction);
                float2 tangent = float2(direction.y, -direction.x);
                float2 centered = input.uv - 0.5;
                float2 uv = float2(dot(centered, tangent), dot(centered, direction)) + 0.5;
                float clock = _PreviewTime < 0.0 ? _Time.y : _PreviewTime;
                float time = clock * _Speed + _Phase;
                float broadFlow = Fbm(float2(uv.x * 3.2 + time * 0.12, uv.y * 2.1 - time));
                float2 warpedUv = uv;
                warpedUv.x += (broadFlow - 0.5) * (0.18 + uv.y * 0.24);
                warpedUv.y += sin((uv.x * 9.0 + time * 1.7) + broadFlow * 5.0) * 0.025;

                float height = 0.34 + Fbm(float2(warpedUv.x * 5.0 - time * 0.16, -time * 0.72)) * 0.62;
                float body = 1.0 - smoothstep(height - 0.14, height + 0.08, warpedUv.y);
                float detail = Fbm(float2(warpedUv.x * 10.0 + broadFlow * 2.0, warpedUv.y * 6.0 - time * 2.25));
                float split = Fbm(float2(warpedUv.x * 18.0 - time * 0.42, warpedUv.y * 11.0 - time * 3.1));
                float baseGlow = 1.0 - smoothstep(0.02, 0.92, warpedUv.y);
                float density = saturate(body * (detail * 0.82 + split * 0.34 + baseGlow * 0.42 - 0.42));
                float edgeFade = smoothstep(0.0, 0.06, uv.x) * (1.0 - smoothstep(0.94, 1.0, uv.x));
                density *= edgeFade * smoothstep(0.0, 0.08, uv.y) * (1.0 - smoothstep(0.82, 1.0, uv.y));

                float heat = saturate(density * 1.4 + baseGlow * 0.28 - uv.y * 0.2);
                half3 color = lerp(_OuterColor.rgb, _MidColor.rgb, smoothstep(0.05, 0.42, heat));
                color = lerp(color, _InnerColor.rgb, smoothstep(0.42, 0.76, heat));
                color = lerp(color, _HotColor.rgb, smoothstep(0.76, 1.0, heat));
                float alpha = smoothstep(0.05, 0.36, density) * _Fade;
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float mask = lerp(1.0, sprite.a, _UseSpriteMask);
                half4 vertexColor = lerp(half4(1.0, 1.0, 1.0, 1.0), input.color, _UseSpriteMask);
                alpha *= mask;
                color = lerp(color, max(color, sprite.rgb), _SpriteContribution * sprite.a);
                alpha = max(alpha, sprite.a * _SpriteContribution * 0.35) * vertexColor.a;
                return half4(color * _Intensity * vertexColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
