#include "../common.fx"

sampler noise : register(s0);

float4 background;

float4 startColor;
float4 endColor;
float4 ringStartColor;
float4 ringEndColor;

float quickTime;
float expandTime;
float ringTime;
float longTime;

float inCubic(float t)
{
    return pow(t, 3);
}
float outCubic(float t)
{
    return 1 - inCubic(1 - t);
}
float inOutCubic(float t)
{
    if (t < .5) 
        return inCubic(t * 2) * .5;
    return 1 - inCubic((1 - t) * 2) * .5;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float n = tex2D(noise, coords + longTime);

    float dist = length(.5 - coords) * 2;
    
        // Create this wobbly kind of expanding circle.
    float radius = outCubic(expandTime) + n * (1 - dist) * quickTime;
	
    float interpolator = saturate(radius - dist - longTime);
	
    float4 bluecolor = lerp(startColor, endColor, expandTime) * (5 - inOutCubic(quickTime) * 4.75);
	
    float4 color = lerp(background, bluecolor, interpolator);
    
        // Add an sort of expanding ring.
    float expandingRing = max(clampedMap(interpolator, .5, .55, 1, 0), 0);
    color += frac(expandingRing) * .2 * startColor * (1 - expandTime);
    
    color *= 1.9;
    
    float shellinterpolator = 1 - abs(.5 - map(interpolator, .65, .9, 1, 0));
    
    float4 outer = lerp(ringStartColor, ringEndColor, inCubic(longTime));
    
        // Interpolate using the oklap colorspace for a better transition.
    color = oklabLerp(color, outer, saturate(shellinterpolator - longTime) * outCubic(ringTime) * interpolator);
    
        // Make sure everything actually vanishes.
    return color * sampleColor * outCubic(1 - longTime);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}