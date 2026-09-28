// Paper grain overlay (art-direction 5, PRG-04): a full-screen pass after post-processing. Static grain
// on the paper, stronger where the image is light (unpainted paper shows grain; ink hides it), plus faint
// horizontal fibre. Used by a FullScreenPassRendererFeature on the URP renderer.
// First, the lantern-radius (bible 4.6, 4.7; PRG-18): in the Greyfold and the Blank everything beyond Wren's
// radius is white paper, and outlines survive only toward the edge of the eye. The grain then lies on the white.
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
            float _OWSBG_Held;   // global: 1 inside an anchored place (HeldState)
            float _OWSBG_Dusk;   // global: warm cast at dusk and dawn (DayCycle)
            float _OWSBG_Night;  // global: cool, darker at night (DayCycle)
            float _OWSBG_Lantern;          // global: how much the room is drawn round her lantern (ClarityMeter)
            float4 _OWSBG_LanternCentre;   // global: her centre (viewport x, y), the radius in viewport heights, the aspect

            half Luma(half3 c) { return dot(c, half3(0.299h, 0.587h, 0.114h)); }

            half3 LanternRadius(float2 uv, half3 col)
            {
                float aspect = max(0.01, _OWSBG_LanternCentre.w);
                float2 d = (uv - _OWSBG_LanternCentre.xy) * float2(aspect, 1.0);
                float k = length(d) / max(1e-4, _OWSBG_LanternCentre.z);    // 1 at the radius
                half outside = smoothstep(0.85h, 1.15h, (half)k);
                // An outline: the image's own edges, kept faintly, and more toward the frame's edge.
                float2 t = _BlitTexture_TexelSize.xy * 1.5;
                half ll = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(t.x, 0)).rgb);
                half lr = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(t.x, 0)).rgb);
                half ld = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, t.y)).rgb);
                half lu = Luma(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, t.y)).rgb);
                half edge = saturate((abs(lr - ll) + abs(lu - ld)) * 8.0h);
                half periphery = smoothstep(0.35h, 0.9h, (half)length((uv - 0.5) * float2(aspect, 1.0)));
                half3 paper = half3(0.97h, 0.96h, 0.93h);
                half3 white = lerp(paper, half3(0.36h, 0.36h, 0.40h), edge * (0.15h + 0.7h * periphery));
                return lerp(col, white, outside * saturate((half)_OWSBG_Lantern));
            }

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
                if (_OWSBG_Lantern > 0.001) col.rgb = LanternRadius(uv, col.rgb);

                float2 px = uv / _BlitTexture_TexelSize.xy;
                half grain = Hash21(floor(px / max(1.0, _Scale))) - 0.5h;
                half fibre = Hash21(floor(float2(px.x / 64.0, px.y * 0.5))) - 0.5h;
                half lum = saturate(dot(col.rgb, half3(0.299h, 0.587h, 0.114h)));
                half amount = _Strength * (0.35h + 0.65h * lum);
                col.rgb += grain * amount + fibre * _Fibre * lum;
                col.rgb = lerp(col.rgb, col.rgb * _PaperTint.rgb, _TintAmount);
                // Anchored: the colour grade locks. Desaturate a third and cast toward brass-blue.
                half3 locked = lerp(lum.xxx, col.rgb, 0.65h) * half3(0.93h, 0.97h, 1.05h);
                col.rgb = lerp(col.rgb, locked, saturate(_OWSBG_Held));
                // The hour: dusk warms the paper, night cools and darkens it (hub-life 2).
                half3 dusk = col.rgb * half3(1.04h, 0.92h, 0.78h);
                col.rgb = lerp(col.rgb, dusk, saturate(_OWSBG_Dusk) * 0.7h);
                half3 night = col.rgb * half3(0.55h, 0.62h, 0.82h);
                col.rgb = lerp(col.rgb, night, saturate(_OWSBG_Night) * 0.8h);
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
