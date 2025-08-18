#include "../spheres.fxh"

sampler lightTexture : register(s0);
sampler shadowTexture : register(s1);

float radius;

float shadowRotation;

float4 shadowColor;
float4 atmosphereColor;
float4 atmosphereShadowColor;

float4 planet(float2 uv, float dist, float3 sp, float shad)
{
    float2 pt = lonlat(sp);
    
    float falloff = step(dist, radius);
    
    return lerp(
        shadowColor * tex2D(shadowTexture, pt),
        tex2D(lightTexture, pt),
        shad) * falloff;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - .5) * 2;
    
    float dist = length(uv);
    
    if (dist > radius)
    {
        float3 sp = sphere(uv, dist, 1);
        float shad = shadow(sp, shadowRotation, 4);
		
        float4 color = atmo(dist, shad, radius, atmosphereColor == 0 ? tex2D(lightTexture, .5) : atmosphereColor, atmosphereShadowColor, 0);
        
        color *= color.a;
        
        color.a = 0;
        
        return color * sampleColor;
    }
    else
    {
        float3 sp = sphere(uv, dist, radius);
        float shad = shadow(sp, shadowRotation);
		
        float4 color = planet(uv, dist, sp, shad);
		
        return color * color.a * sampleColor;
    }
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}