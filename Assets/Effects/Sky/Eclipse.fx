#include "../common.fx"

sampler doesAnyoneTrulyNeedASampler : register(s0);
float uTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float veins = 1 - coronaries(coords * 3, uTime);
    
    float dist = saturate(1 - length(coords - 0.5) * 2.);
    
    float mixed = saturate(lerp(dist * 1.2, saturate(veins * 1.2), .5) - .5) * 12.;
    
    return mixed * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}