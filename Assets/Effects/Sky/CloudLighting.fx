sampler Cloud : register(s0);
sampler Light : register(s1);

float2 screenSize;

float pixel;

float3 lighting(float4 lightColor, float4 cloud)
{
        // Get the dark parts of the clouds
    float shadows = 1 - cloud.r;
    
        // Combine the distance with the dark parts to make it look as if light is bleeding through.
    float glow = ((shadows * 2.7) + (cloud.r * .65));
    
    float4 inner = lightColor * glow;
    
    inner *= inner.a;
    
    return saturate(inner.rgb);
}

float4 avgLight(float2 screenCoords)
{
    float2 uv = screenCoords / screenSize;
    float3 e = float3(pixel / screenSize, 0);
    
    float4 center = tex2D(Light, uv);
    
    if (center.a <= 0)
        return 0;
    
    float4 up = tex2D(Light, uv - e.zy);
    float4 down = tex2D(Light, uv + e.zy);
    float4 right = tex2D(Light, uv - e.xz);
    float4 left = tex2D(Light, uv + e.xz);
    
    return (center + up + down + right + left) * .2;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float4 cloud = tex2D(Cloud, coords);
    
    float4 color = cloud * sampleColor;
    
    color.rgb += lighting(avgLight(screenCoords), cloud) * color.a;
    
    color = saturate(color);
    
    return color;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}