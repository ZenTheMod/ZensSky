#include "../Compat/realisticSky.fxh"

sampler star : register(s0);
sampler atmosphere : register(s1);

float2 screenSize;
float2 sunPosition;
float distanceFadeoff;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float2 screenPosition : SV_POSITION, float4 sampleColor : COLOR0) : COLOR0
{
    float2 screenCoords = screenPosition / screenSize;
    
    float opactity = StarOpacity(screenPosition, coords, sunPosition, tex2D(atmosphere, screenCoords).rgb, distanceFadeoff);
    
    return tex2D(star, coords) * sampleColor * opactity;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
