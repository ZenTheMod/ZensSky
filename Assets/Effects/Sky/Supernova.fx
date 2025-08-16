#include "../common.fxh"
#include "../Compat/realisticSky.fxh"

sampler noise : register(s0);
sampler atmosphere : register(s1);

float4 background;

float4 startColor;
float4 endColor;
float4 ringStartColor;
float4 ringEndColor;

float quickTime;
float expandTime;
float ringTime;
float longTime;

float globalTime;

float2 offset;

float2 screenSize;
float2 sunPosition;
float distanceFadeoff;

bool usesAtmosphere;

float4 supernova(float2 coords)
{
    float n = tex2D(noise, coords * 3 + expandTime + offset);

    float dist = length(.5 - coords) * 2;
    
        // Create this wobbly kind of expanding circle.
    float radius = outCubic(expandTime) + n * (1 - dist) * quickTime;
	
    float interpolator = saturate(radius - dist - longTime);
    
    float4 explosionColor = oklabLerp(startColor, endColor, quickTime);
	
    float4 explosion = explosionColor * (5 - inOutCubic(quickTime) * 4.75);
	
    float4 color = oklabLerp(background, explosion, interpolator);
    
        // Add an sort of expanding ring.
    float expandingRing = clampedMap(abs(.4 - interpolator), 0, .03, 1, 0);
    color += frac(expandingRing) * .6 * explosionColor * (1 - expandTime);
    
    color *= 1.2;
    
    float shellinterpolator = 1 - abs(.5 - map(interpolator, .65, .9, 1, 0));
    
        // Add a small vein-like effect. (Really I'm just mashing shit together.)
    shellinterpolator *= 1.5 - coronaries(coords * 4 + offset, globalTime * 0.00001);
    
    float4 outer = lerp(ringStartColor, ringEndColor, inCubic(longTime));
    
        // Interpolate using the oklap colorspace for a better transition.
    color = oklabLerp(color, outer, saturate(shellinterpolator - longTime) * outCubic(ringTime) * interpolator);
    
        // Add a small glowing "star" at its center
    color = oklabLerp(color, startColor, (1 - longTime) * quickTime * saturate(inCubic(1 - dist * 7)));
        
        // Add a vauge dust cloud.
    color += endColor * min(outCubic(.2 * interpolator * n) * expandTime, .1);
    
    return color * outCubic(1 - longTime) * color.a;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 screenPosition : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = supernova(coords);
    
    float2 screenCoords = screenPosition / screenSize;
    
    float opactity = 1;
    
    if (usesAtmosphere)
        opactity = StarOpacity(screenPosition, coords, sunPosition, tex2D(atmosphere, screenCoords).rgb, distanceFadeoff);
    
    return color * sampleColor * opactity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}