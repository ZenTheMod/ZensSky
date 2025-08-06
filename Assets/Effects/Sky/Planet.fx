#include "../spheres.fx"

sampler tex : register(s0);

float radius;

float shadowRotation;

float4 shadowColor;
float4 atmosphereColor;
float4 atmosphereShadowColor;

float4 planet(float2 uv, float dist, float3 sp, float shad)
{
    if (dist > radius)
        return float4(0, 0, 0, 0);
    
    float2 pt = lonlat(sp);
    
    float falloff = clampedMap(dist, radius - .03, radius, 1, 0);
    
    return lerp(shadowColor, tex2D(tex, pt), shad) * falloff;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - .5) * 2;
    
    float dist = length(uv);
    
    float3 sp = sphere(uv, dist, radius);
    
    float shad = shadow(sp, shadowRotation);
    
    float4 inner = planet(uv, dist, sp, shad);
    
    float4 outer = atmo(dist, shad, radius, atmosphereColor, atmosphereShadowColor);
    
    float4 color = (inner + outer) * sampleColor;
    
    return color * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}