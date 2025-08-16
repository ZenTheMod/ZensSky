#ifndef COMPAT_FX
#define COMPAT_FX

float StarOpacity(float2 screenPosition, float2 coords, float2 sunPosition, float3 atmosphere, float distanceFadeoff)
{
    float distanceSqrFromSun = dot(screenPosition - sunPosition, screenPosition - sunPosition);
    float atmosphereInterpolant = dot(atmosphere, 0.333);
    float opacity = saturate(1 - smoothstep(57600, 21500, distanceSqrFromSun / distanceFadeoff) - atmosphereInterpolant * 2.05);
    
    return opacity;
}

#endif