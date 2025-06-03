#include "../common.fx"

sampler noise : register(s0);

float4 startColor;
float4 endColor;

float time;

float scale;

float fbm1(float2 uv)
{
    float freq = 1.0;
    float ret = 0.;
   
    for (float i = 0; i < 3; i++)
    {
        ret += 1 - tex2D(noise, uv * scale * freq + float2(-time, 0)).x;
        freq *= .5;
    }
    return ret * .3333;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
        // Convert the colorspace into oklab.
    float4 blue = float4(toOklab(startColor.rgb), startColor.a);
    float4 red = float4(toOklab(endColor.rgb), endColor.a);
    
    float n = fbm1(coords);
    
    float4 color = lerp(blue * .96, red * .2, outCubic(saturate(coords.x * 8))) * n;
    
    color *= 1 - (abs(coords.y - .5) * 2);
    
    color *= saturate(-(pow((coords.y - .5), 2) - 4 * 3 * (coords.x))) * 5;
    
    color *= outCubic(1 - coords.x);
    
    color += (max(n, outCubic(1 - coords.x)) * pow(color * 3, 1.3));
    
    color = float4(toRGB(color.rgb) * color.a, color.a);
    
    color = pow(color, 1.2);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}