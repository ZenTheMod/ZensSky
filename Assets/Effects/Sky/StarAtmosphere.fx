sampler star : register(s0);
sampler atmosphere : register(s1);

float alpha;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
        // This is the exact line used in realistic sky to decrease the brightness based on the atmosphere effect.
    float atmosphereInterpolant = dot(tex2D(atmosphere, coords).rgb, 0.333);
    
    float opacity = saturate(alpha - atmosphereInterpolant * 2.05);
    
    return tex2D(star, coords) * sampleColor * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
