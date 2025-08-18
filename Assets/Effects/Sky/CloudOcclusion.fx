sampler Texture : register(s0);

struct PSOutput
{
    float4 Background : COLOR0;
    float4 Occluders : COLOR1;
};

PSOutput PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0)
{
    PSOutput output;
    
    float4 color = tex2D(Texture, coords);
    
    color *= sampleColor;
    
    output.Background = color;
    output.Occluders = color.a;
    
    return output;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}