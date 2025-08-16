sampler Occluders : register(s0);
sampler Body : register(s1);

float2 lightPosition;
float4 lightColor;
float lightSize;

bool shouldSample;

float2 screenSize;

int sampleCount;

static const float4x4 bayer = float4x4(
    0, 8, 2, 10,
    12, 4, 14, 6,
    3, 11, 1, 9,
    15, 7, 13, 5.6) / 16;

float blur(float2 uv, float2 screenCoords, int samples, float2 lightpos, float dither)
{
    float2 dist = (lightpos - screenCoords) / min(screenSize.x, screenSize.y);
    
    float2 dtc = dist * (1 / samples);
    
    float2 offset = dither * dtc;
    
    float light = 1 - saturate(length(dist) * 3.4 / max(lightSize, .001));
    
    if (light <= 0)
        return 0;
    
    float occ = 0;
    
    samples = max(samples, 8);
    
    [unroll(128)]
    for (int i = 0; i < samples; i++)
    {
        uv += dtc;
        
        occ += light -
        	tex2D(Occluders, uv + offset).a;
    }
    
    occ /= samples;
    
    return occ;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float2 bayeruv = frac(screenCoords.xy * .25) * 4;
    
    float dither = bayer[bayeruv.x][bayeruv.y];
    
    float4 color = lightColor *
            blur(coords, screenCoords, sampleCount, lightPosition, dither);
    
    if (shouldSample)
        color *= tex2D(Body, .5);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}