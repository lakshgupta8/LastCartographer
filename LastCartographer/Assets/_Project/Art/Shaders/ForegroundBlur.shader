// Foreground-only depth of field (art-direction 3, PRG-04): everything nearer than the gameplay plane
// blurs, growing with how far in front it is; the plane and everything behind it stay sharp (the far
// layers are handled by the volume's Gaussian DoF). A gather blur where a tap counts when either the
// centre's or the tap's own blur radius reaches it, so foreground paper bleeds over the plane the way
// a real near-field blur does. Runs before post-processing as a FullScreenPassRendererFeature.
Shader "OWSBG/FullScreen/ForegroundBlur"
{
    Properties
    {
        _FocusDistance ("Focus Distance (camera to plane)", Float) = 18
        _Range ("Blur Range (units in front)", Float) = 7
        _MaxRadius ("Max Radius (px)", Range(0, 16)) = 7
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "ForegroundBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _FocusDistance;
            float _Range;
            float _MaxRadius;

            // 0 at or behind the plane, 1 at _Range units in front of it.
            float Coc(float2 uv)
            {
                float raw = SampleSceneDepth(uv);
                float eye = LinearEyeDepth(raw, _ZBufferParams);
                return saturate((_FocusDistance - eye) / max(0.01, _Range));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 centre = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float cocC = Coc(uv);
                float2 texel = _BlitTexture_TexelSize.xy;

                static const float2 dirs[8] =
                {
                    float2(1, 0), float2(0.7071, 0.7071), float2(0, 1), float2(-0.7071, 0.7071),
                    float2(-1, 0), float2(-0.7071, -0.7071), float2(0, -1), float2(0.7071, -0.7071)
                };

                half3 sum = centre.rgb;
                float wsum = 1.0;
                [unroll]
                for (int ring = 1; ring <= 2; ring++)
                {
                    float len = ring * 0.5;
                    [unroll]
                    for (int i = 0; i < 8; i++)
                    {
                        float2 tuv = uv + dirs[i] * len * _MaxRadius * texel;
                        float reach = max(cocC, Coc(tuv));
                        float w = step(len, reach + 0.001);
                        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, tuv).rgb * w;
                        wsum += w;
                    }
                }
                return half4(sum / wsum, centre.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
