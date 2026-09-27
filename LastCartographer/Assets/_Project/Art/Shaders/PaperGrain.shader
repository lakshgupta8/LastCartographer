// Paper grain overlay (art-direction 5, PRG-04): a full-screen pass after post-processing. Static grain
// on the paper, stronger where the image is light (unpainted paper shows grain; ink hides it), plus faint
// horizontal fibre. Used by a FullScreenPassRendererFeature on the URP renderer.
Shader "OWSBG/FullScreen/PaperGrain"
{
    Properties
    {
        _Strength ("Grain Strength", Range(0, 0.3)) = 0.07
        _Scale ("Grain Size (px)", Range(1, 8)) = 2
        _Fibre ("Fibre Strength", Range(0, 0.1)) = 0.02
        _PaperTint ("Paper Tint", Color) = (1.0, 0.98, 0.94, 1)
        _TintAmount ("Tint Amount", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "PaperGrain"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;
            float _Scale;
            float _Fibre;
            half4 _PaperTint;
            float _TintAmount;

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 px = uv / _BlitTexture_TexelSize.xy;
                half grain = Hash21(floor(px / max(1.0, _Scale))) - 0.5h;
                half fibre = Hash21(floor(float2(px.x / 64.0, px.y * 0.5))) - 0.5h;
                half lum = saturate(dot(col.rgb, half3(0.299h, 0.587h, 0.114h)));
                half amount = _Strength * (0.35h + 0.65h * lum);
                col.rgb += grain * amount + fibre * _Fibre * lum;
                col.rgb = lerp(col.rgb, col.rgb * _PaperTint.rgb, _TintAmount);
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
