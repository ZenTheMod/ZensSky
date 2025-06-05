#include "../common.fx"

sampler sky : register(s0);

float2 screenSize;
float2 pixelSize;

float steps;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 size = screenSize / pixelSize;
    
    coords = floor(coords * size) / size;
    
    float4 color = tex2D(sky, coords);
    
    color.rgb = RGBtoHSL(color.rgb);
    
    color.rgb = round(color.rgb * steps) / steps;
    
    color.rgb = HSLtoRGB(color.rgb);
    
    color.a = max(color.a, (color.r + color.g + color.b) * .333);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}