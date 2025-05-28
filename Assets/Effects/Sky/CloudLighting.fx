sampler Clouds : register(s0);

float2 ScreenSize;

bool DrawSun;
float2 SunPosition;
float4 SunColor;

bool DrawMoon;
float2 MoonPosition;
float4 MoonColor;

float4 Lighting(float2 lightPosition, float2 screenPosition, float4 lightColor, float4 cloud)
{
    float2 screen = screenPosition / ScreenSize;
    float2 light = lightPosition / ScreenSize;
    
        // Get the actual length.
    float distlight = saturate(1 - (length(light - screen) * 3.3));
    
        // Get the dark parts of the clouds
    float shadows = 1 - cloud.r;
    
        // Combine the distance with the dark parts to make it look as if light is bleeding through.
    float glow = distlight * ((shadows * 3.3) + (cloud.r * 0.65));
    
    float4 color = cloud;
    
    return lightColor * 2. * distlight * glow;
}

    // https://www.shadertoy.com/view/tX2Xzc
        // This shader uses SV_POSITION to grab the coordinate on the screen of the pixel being drawn, rather than the texture coordinate.
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float4 cloud = tex2D(Clouds, coords);
    
    float4 sun = float4(0, 0, 0, 0);
    if (DrawSun)
        sun = Lighting(SunPosition, screenCoords, SunColor, cloud);
    
    float4 moon = float4(0, 0, 0, 0);
    if (DrawMoon)
        moon = Lighting(MoonPosition, screenCoords, MoonColor, cloud);
    
    float4 color = cloud * sampleColor;
    
    return color + (sun * color.a) + (moon * color.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}