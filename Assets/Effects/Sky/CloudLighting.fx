sampler Cloud : register(s0);
sampler Light : register(s1);

float2 screenSize;

float4 lighting(float4 lightColor, float4 cloud)
{
    float light = lightColor.a;
    
        // Get the dark parts of the clouds
    float shadows = 1 - cloud.r;
    
        // Combine the distance with the dark parts to make it look as if light is bleeding through.
    float glow = light * ((shadows * 2.7) + (cloud.r * .65));
    
    float4 inner = 2. * light * glow;
    inner.rgb *= lightColor.rgb;
    
    return saturate(inner);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 sampleColor : COLOR0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float4 cloud = tex2D(Cloud, coords);
    
    float4 light = lighting(tex2D(Light, screenCoords / screenSize), cloud);
    
    float4 color = cloud * sampleColor;
    
    return color + (light * color.a) * step(coords.x, .5);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}