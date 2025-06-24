#include "../common.fx"

sampler img : register(s0);

float2 screenSize;
float2 pixelSize;

float steps;

float4 PixelateAndQuantize(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 size = screenSize / pixelSize;
    
    coords = floor(coords * size) / size;
    
    float4 color = tex2D(img, coords);
    
    color.rgb = RGBtoHSL(color.rgb);
    
    color.rgb = round(color.rgb * steps) / steps;
    
    color.rgb = HSLtoRGB(color.rgb);
    
    color.a = max(color.a, (color.r + color.g + color.b) * .333);
    
    return color;
}

float4 Pixelate(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 size = screenSize / pixelSize;
    
    coords = floor(coords * size) / size;
    
    float4 color = tex2D(img, coords);
    
    return color;
}

technique Technique1
{
    pass PixelateAndQuantizePass
    {
        PixelShader = compile ps_2_0 PixelateAndQuantize();
    }

    pass PixelatePass
    {
        PixelShader = compile ps_2_0 PixelateAndQuantize();
    }
}