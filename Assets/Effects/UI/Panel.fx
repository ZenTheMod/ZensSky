sampler Panel : register(s0);
sampler RT : register(s1);
sampler Gradient : register(s2);

float4 Source;

float4 PixelShaderFunction(float2 coords : SV_POSITION, float2 textureCoords : TEXCOORD0) : COLOR0
{
    float2 resolution = Source.xy;
    float2 position = Source.zw;

    coords -= position;
    
    coords = floor(coords / 2) / (resolution / 2);
    
    float3 col = tex2D(RT, coords).rgb;
    
    float gray = dot(col, float3(0.2126, 0.7152, 0.0722));
    
    float3 color = tex2D(Gradient, float2(0, 1 - gray));
    
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