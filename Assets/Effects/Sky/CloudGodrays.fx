sampler Occluders : register(s0);
sampler Body : register(s1);

float2 lightPosition;
float4 lightColor;
float lightSize;

bool useTexture;

float2 screenSize;

int sampleCount;

float blur(float2 uv, float2 lightuv, int samples)
{
    float2 screen = screenSize / min(screenSize.x, screenSize.y);
    
    float2 dir = (lightuv - uv) * screen;
    
        // Use 1. to avoid integer division.
    float2 dtc = dir * (1. / samples);
    
    float size = 3.9 / max(lightSize, .001);
    
    float occ = 0;
    
    samples = max(samples, 8);
    
    [unroll(64)]
    for (int i = 0; i < samples; i++)
    {
        uv += dtc;
        
        float light = saturate(length((lightuv - uv) * screen) * size);
    
        light *= light;
        light = 1 - light;
    
        occ += light -
        	tex2D(Occluders, uv);
    }
    
    occ /= samples;
    
    return occ;
}

float light(float2 uv, float2 lightuv)
{
    float2 screen = screenSize / min(screenSize.x, screenSize.y);
    
    float2 dir = (lightuv - uv) * screen;
    
    float size = 3.9 / max(lightSize, .001);
    
    float light = saturate(length((lightuv - uv) * screen) * size);
    
    light *= light;
    light = 1 - light;
    
    return light;
}

float4 SampledGodrays(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float2 bayeruv = frac(screenCoords.xy * .25) * 4;
    
    float4 color = lightColor;
    
    float2 lightuv = lightPosition / screenSize;
    float2 uv = coords;
    
    color.a *= blur(uv, lightuv, sampleCount);
    
    if (useTexture)
        color *= tex2D(Body, .5);
    
    return color;
}

float4 SimpleLight(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float2 bayeruv = frac(screenCoords.xy * .25) * 4;
    
    float4 color = lightColor;
    
    float2 lightuv = lightPosition / screenSize;
    float2 uv = coords;
    
    color.a *= light(uv, lightuv);
    
    if (useTexture)
        color *= tex2D(Body, .5);
    
    return color;
}

technique Technique1
{
    pass Godrays
    {
        PixelShader = compile ps_3_0 SampledGodrays();
    }

    pass Light
    {
        PixelShader = compile ps_3_0 SimpleLight();
    }
}