#ifndef UNDERWATER_COMMON_INCLUDED
#define UNDERWATER_COMMON_INCLUDED

half4 _UnderwaterFogColor;
half4 _UnderwaterColorSurface;
half4 _UnderwaterColorDeep;
half4 _UnderwaterSunGlow;
float _UnderwaterFogDensity;
float _UnderwaterFadeStart;
float _UnderwaterFadeEnd;
float _UnderwaterLevel;
float _UnderwaterAbsorbTint;
float _UnderwaterSurgeAmp;
float _UnderwaterSurgeSpeed;
float4 _UnderwaterSurgeDir;
half4 _UnderwaterEncrustA;
half4 _UnderwaterEncrustB;
float _UnderwaterEncrustAmount;
float _UnderwaterEncrustScale;

half3 UnderwaterBackground(float3 viewDir)
{
    half3 col = lerp(_UnderwaterFogColor.rgb, _UnderwaterColorSurface.rgb,
                     smoothstep(0.0, 0.7, viewDir.y));
    col = lerp(col, _UnderwaterColorDeep.rgb,
               smoothstep(0.0, 0.6, -viewDir.y));

    float3 L = _MainLightPosition.xyz;
    float sunAmount = saturate(dot(viewDir, L));
    col += _UnderwaterSunGlow.rgb *
           (pow(sunAmount, 12.0) * 0.5 + pow(sunAmount, 90.0) * 0.8);
    return col;
}

float3 UnderwaterAbsorption()
{
    float3 w = _UnderwaterFogColor.rgb;
    float peak = max(max(w.r, w.g), max(w.b, 1e-4));
    float3 hue = saturate(w / peak);
    return _UnderwaterFogDensity * (1.0 + _UnderwaterAbsorbTint * (1.0 - hue));
}

float UnderwaterHardFade(float dist)
{
    float fadeEnd   = _UnderwaterFadeEnd > 0.01 ? _UnderwaterFadeEnd : 1e5;
    float fadeStart = min(_UnderwaterFadeStart, fadeEnd - 0.01);
    return smoothstep(fadeStart, fadeEnd, dist);
}

float UnderwaterFog(float3 positionWS)
{
    float dist   = distance(positionWS, _WorldSpaceCameraPos);
    float expFog = 1.0 - exp(-pow(dist * _UnderwaterFogDensity, 2.0));
    return max(expFog, UnderwaterHardFade(dist));
}

half3 ApplyUnderwaterMedium(half3 color, float3 positionWS, float3 viewDirWS)
{
    float dist = distance(positionWS, _WorldSpaceCameraPos);
    half3 bg   = UnderwaterBackground(-viewDirWS);

    float3 a = UnderwaterAbsorption();
    float3 T = exp(-pow(dist * a, 2.0));

    half3 lit = color * T + bg * (1.0 - T);
    return lerp(lit, bg, UnderwaterHardFade(dist));
}

half UnderwaterCaustics(float2 uv, float time)
{
    const float sharpness = 0.005;
    float2 p = fmod(uv, TWO_PI) - 250.0;
    float2 i = p;
    float  c = 1.0;

    UNITY_UNROLL
    for (int n = 0; n < 3; n++)
    {
        float t = time * (1.0 - (3.5 / float(n + 1)));
        i = p + float2(cos(t - i.x) + sin(t + i.y),
                       sin(t - i.y) + cos(t + i.x));
        c += 1.0 / length(float2(p.x / (sin(i.x + t) / sharpness),
                                 p.y / (cos(i.y + t) / sharpness)));
    }
    c = 1.17 - pow(abs(c / 3.0), 1.4);
    return pow(abs(c), 8.0);
}

half3 UnderwaterCausticsRGB(float2 uv, float time, float chroma)
{
    half c    = saturate(UnderwaterCaustics(uv, time));
    half edge = saturate(fwidth(c) * chroma);
    return half3(saturate(c + edge), c, saturate(c - edge));
}

float UwHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float UwValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = UwHash21(i);
    float b = UwHash21(i + float2(1.0, 0.0));
    float c = UwHash21(i + float2(0.0, 1.0));
    float d = UwHash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

half3 RockSurfaceDetail(half3 albedo, float3 positionWS, float scale,
                        float mottleStrength, float strataScale, float strataStrength)
{
    float2 q = float2(positionWS.x + positionWS.y * 0.6,
                      positionWS.z - positionWS.y * 0.6) * scale;

    float nBroad = UwValueNoise(q);
    float nFine  = UwValueNoise(q * 3.1 + 4.7);
    float nLow   = UwValueNoise(q * 0.32 + 19.3);

    float mottle = nBroad * 0.62 + nFine * 0.38;
    albedo *= lerp(1.0, 0.58 + 0.88 * mottle, mottleStrength);

    float band   = 0.5 + 0.5 * sin((positionWS.y + (nLow - 0.5) * 1.6) * strataScale);
    float bedded = saturate(nLow * 1.7);
    albedo *= lerp(1.0, 0.76 + 0.38 * band, strataStrength * bedded);

    return albedo;
}

half3 ApplyEncrustation(half3 color, float3 positionWS, half3 N, half ao, half3 lighting)
{
    if (_UnderwaterEncrustAmount <= 0.001) return color;

    float scale = max(_UnderwaterEncrustScale, 0.01);
    float2 q = positionWS.xz * scale + positionWS.y * (scale * 0.35);
    float patch = UwValueNoise(q);
    float fine  = UwValueNoise(q * 2.9 + 7.1);

    float light = saturate(N.y * 0.5 + 0.5);
    float cover = saturate((patch * 0.7 + fine * 0.3 - 0.44) * 3.4);
    cover *= light * ao * _UnderwaterEncrustAmount;

    half3 crust = lerp(_UnderwaterEncrustA.rgb, _UnderwaterEncrustB.rgb, saturate(fine * 1.5));
    return lerp(color, crust * lighting, saturate(cover));
}

float3 UnderwaterSurgeWorld(float heightAboveBase, float3 pivotWS, float flexibility)
{
    float2 dir = normalize(_UnderwaterSurgeDir.xy + 1e-5);
    float phase = dot(pivotWS.xz, float2(0.7, 1.3));
    float t = _Time.y * _UnderwaterSurgeSpeed + phase;

    float s = sin(t) * 0.75 + sin(t * 2.13 + 1.7) * 0.25;
    float bend = max(heightAboveBase, 0.0) * flexibility * _UnderwaterSurgeAmp;
    return float3(dir.x * s, 0.0, dir.y * s) * bend;
}

float3 UnderwaterObjectPivot()
{
    return float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
}

#endif
