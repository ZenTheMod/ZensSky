sampler Clouds : register(s0);

float2 ScreenSize;

float2 SunPosition;
float4 SunColor;

    // https://www.shadertoy.com/view/tX2Xzc
        // This shader uses SV_POSITION to grab the coordinate on the screen of the pixel being drawn, rather than the texture coordinate.
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float2 screen = screenCoords / ScreenSize;
    float2 sun = SunPosition / ScreenSize;
    
    float4 clouds = tex2D(Clouds, coords);
    
        // Get the actual length.
    float distsun = saturate(1 - (length(sun - screen) * 3.5));
    
        // Get the dark parts of the clouds
    float shadows = 1 - clouds.r;
    
        // Combine the distance with the dark parts to make it look as if light is bleeding through.
    float glow = distsun * ((shadows * 3.3) + (clouds.r * 0.65));
    
    float4 color = clouds * sampleColor;
    
        // Mashy Mashy.
    return color + (SunColor * 2. * color.a * distsun * glow);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}