// Old-cartoon film look for the built-in pipeline: grain, scratches, dust, flicker, sepia, vignette, flash and an iris wipe.
// Everything is quantised to the 480x270 virtual pixel grid so it matches the pixel art.
Shader "Hidden/FilmFilter"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _T, _Grain, _Scratch, _Dust, _Vignette, _Sepia, _Flicker, _Iris, _Flash;
            float4 _IrisCenter, _FlashColor, _Ink;

            // sine-free hash (Hoskins): stays uniform for large inputs, unlike frac(sin(..)) which clumps on GPUs
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 px = floor(uv * float2(480.0, 270.0));
                float frame = fmod(floor(_T * 12.0), 509.0);
                fixed4 c = tex2D(_MainTex, uv);

                // warm sepia wash
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, float3(lum * 1.08, lum * 0.96, lum * 0.80), _Sepia);

                // projector flicker
                c.rgb *= 1.0 + (hash(float2(frame, 3.1)) - 0.5) * _Flicker;

                // per-virtual-pixel grain
                c.rgb += (hash(px + frame * 17.13) - 0.5) * _Grain;

                // Scratches were 1 virtual pixel wide and read as a seam. Dust was a hard 2x2 square.
                // A rare soft disc instead, faint enough that it does not look like a dead pixel.
                float2 cell = floor(px / 7.0);
                float2 f = frac(px / 7.0) - 0.5;
                float d = hash(cell + frame * 5.71);
                float blob = smoothstep(0.42, 0.05, length(f));
                if (d > 1.0 - _Dust * 3.0) c.rgb = lerp(c.rgb, _Ink.rgb, blob * 0.22);

                // vignette
                float2 v = (uv - 0.5) * float2(1.0, 0.75);
                c.rgb *= 1.0 - _Vignette * smoothstep(0.22, 0.62, length(v));

                // full-screen flash
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _Flash);

                // iris: radius in screen-height units, measured on the virtual pixel grid
                float2 q = (px + 0.5 - _IrisCenter.xy * float2(480.0, 270.0)) / 270.0;
                if (length(q) > _Iris) c.rgb = _Ink.rgb;

                c.a = 1.0;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
