sampler Panel : register(s0);
sampler Sky : register(s1);
sampler Palette : register(s2);

float4 Source;

float4 PixelShaderFunction(float2 coords : SV_POSITION, float2 textureCoords : TEXCOORD0) : COLOR0
{
    float2 resolution = Source.xy;
    float2 position = Source.zw;

    coords = coords - position;
    
    coords = floor(coords / 2) / (resolution / 2);
    
        // The base texture is black and white anyway
    float gray = tex2D(Sky, coords).r;
    
        // I'm inverting it so it the stars look like bits of ink.
    float3 color = tex2D(Palette, float2(0, 1 - gray));
    
    float alpha = tex2D(Panel, textureCoords).a;
    return float4(color, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}