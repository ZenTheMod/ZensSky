sampler Occluders : register(s0);
sampler Body : register(s1);

float2 lightPosition;
float4 lightColor;
float lightSize;

bool useTexture;

float2 screenSize;

int sampleCount;

float blur(float2 uv, float2 screenCoords, int samples, float2 lightpos)
{
    float2 dist = (lightpos - screenCoords) / min(screenSize.x, screenSize.y);
    
        // Use 1. to avoid integer division.
    float2 dtc = dist * (1. / samples);
    
    float size = 3.9 / max(lightSize, .001);
    
    float light = saturate(length(dist) * size);
    
    light *= light;
    light = 1 - light;
    
    if (light <= 0)
        return 0;
    
    float occ = 0;
    
    samples = max(samples, 8);
    
    [unroll(64)]
    for (int i = 0; i < samples; i++)
    {
        uv += dtc;
        
        occ += light -
        	tex2D(Occluders, uv);
    }
    
    occ /= samples;
    
    return occ;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float2 bayeruv = frac(screenCoords.xy * .25) * 4;
    
    float4 color = lightColor;
    
    color.a *= blur(coords, screenCoords, sampleCount, lightPosition);
    
    if (useTexture)
        color *= tex2D(Body, .5);
    
    return color;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}