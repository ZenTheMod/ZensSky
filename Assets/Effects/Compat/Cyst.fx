#include "../spheres.fx"

sampler tex : register(s0);

float atmosphereRange;

float shadowRotation;

float4 shadowColor;
float4 atmosphereColor;
float4 atmosphereShadowColor;

static const float radius1 = .71;
static const float radius2 = .32;

static const float2 pos1 = float2(.24, -.24);
static const float2 pos2 = float2(-.6, .6);

float calcdist(float2 uv)
{
    float dist = 1 / (pow(abs(uv.x), 2.) + pow(abs(uv.y), 2.));
    return dist;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - .5) * 2;
    
        // These will act as our "metamoons."
    float2 uv1 = (uv + pos1) / radius1;
    float dist1 = calcdist(uv1);
    
    float2 uv2 = (uv + pos2) / radius2;
    float dist2 = calcdist(uv2);
    
        // Sum the distances.
    float dist = dist1;
    dist += dist2;
    
        // Mash everything together.
    float3 sp1 = sphere(uv1, 1 / dist, 1) * saturate(dist1);
    float3 sp2 = sphere(uv2, 1 / dist, 1) * saturate(dist2);
    
        // This is the nicest way I've found to blend the two normal maps.
    float3 sum = lerp(sp1, sp2, ((-saturate(dist1)) + saturate(dist2) + 1) * .5);
    
    float shad = shadow(sum, shadowRotation);
    
    float2 pt = lonlat(sum);
    float falloff = clampedMap(1 / dist, .97, 1, 1, 0);
    
        // Then calculate the colors like usual.
    float4 inner = lerp(shadowColor, tex2D(tex, pt), shad) * falloff;
    
    float4 outer = atmo(dist, shad, 1, atmosphereRange, atmosphereColor, atmosphereShadowColor);
    
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