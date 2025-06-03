#include "../common.fx"

sampler tex : register(s0);

float radius;
float atmosphereRange;

float shadowRotation;

float4 shadowColor;
float4 atmosphereColor;
float4 atmosphereShadowColor;

float4 sphere(float2 uv, float dist, float radius)
{
    float z = radius * sin(acos(dist / radius));
    float3 sp = float3(uv, z);
    
        // mfw the * operator ceases to function correctly
    float3 sphererot = mul(sp, mul(rotateX(-PIOVER2), rotateZ(PIOVER2)));
    
    float shadow = outCubic(dot(sphererot, mul(float3(0, 1, 0), rotateZ(TAU - PIOVER2 + shadowRotation))));
    
    shadow = saturate(shadow);
    
    return float4(sphererot, shadow);
}

float4 planet(float2 uv, float dist)
{
    if (dist > radius)
        return float4(0, 0, 0, 0);
    
    float4 sp = sphere(uv, dist, radius);
    
    float3 sphererot = sp.xyz;
    float shadow = sp.w;
    
    float2 pt = lonlat(sphererot);
    
    float falloff = clampedMap(dist, radius - .03, radius, 1, 0);
    
        // Being safe with the texture coords here.
    return lerp(shadowColor, tex2D(tex, pt), shadow) * falloff;
}

float4 atmo(float2 uv, float dist)
{
    float4 sp = sphere(uv, dist, radius);
    
    float3 sphererot = sp.xyz;
    float shadow = sp.w;
    
        // Bullshit.
    float atmo = inCubic(1 - abs(.5 - clampedMap(dist, radius - atmosphereRange, radius + atmosphereRange, 0, 1)));
		
    float4 atmoColor = lerp(atmosphereShadowColor, atmosphereColor, shadow);
		
    return atmoColor * atmo;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - .5) * -2;
    
    float dist = length(uv);
    
    float4 inner = planet(uv, dist);
    
    float4 outer = atmo(uv, dist);
    
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