sampler Form : register(s0);
sampler Void : register(s1);

float uTime;

const float2x2 funny = float2x2(cos(1 + float4(0, 33, 11, 0)));

    // Voodoo.
float coronaries(float2 uv, float time)
{
    float2 a = float2(0, 0);
    float2 res = float2(0, 0);
    float s = 12;
    
    for (float j = 0; j < 12; j++)
    {
        uv = mul(uv, funny);
        a = mul(a, funny);
        
        float2 L = uv * s + j + a - time;
        a += cos(L);
        
        res += (.5 + .5 * sin(L)) / s;
        
        s *= 1.2;
    }
    
    return res.x + res.y;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float veins = 1 - coronaries(coords * 3, uTime);
    
    float dist = saturate(1 - length(coords - 0.5) * 2.);
    
    float mixed = saturate(lerp(dist * 1.2, saturate(veins * 1.2), .5) - .5) * 8.;
    
    return mixed * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}