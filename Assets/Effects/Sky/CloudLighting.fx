sampler Clouds : register(s0);
sampler Moon : register(s1);

bool DrawSun;
float2 SunPosition;
float4 SunColor;

bool DrawMoon;
float2 MoonPosition;
float4 MoonColor;

float4 Lighting(float2 lightPosition, float2 screenPosition, float2 uv, float4 lightColor, float4 cloud)
{
    float2 screen = screenPosition / ScreenSize;
    float2 light = lightPosition / ScreenSize;
    
    float dist = length(light - screen);
    
    float distlight = saturate(1 - (dist * 3.4));
    
        // Get the dark parts of the clouds
    float shadows = 1 - cloud.r;
    
        // Combine the distance with the dark parts to make it look as if light is bleeding through.
    float glow = distlight * ((shadows * 2.7) + (cloud.r * .65));
    
    float4 inner = lightColor * 2. * distlight * glow;
    
    return saturate(inner);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float2 screenCoords : SV_POSITION) : COLOR0
{
    float4 cloud = tex2D(Clouds, coords);
    
    float4 sun = float4(0, 0, 0, 0);
    if (DrawSun)
        sun = Lighting(SunPosition, screenCoords, coords, SunColor, cloud);
    
    float4 moon = float4(0, 0, 0, 0);
    if (DrawMoon)   // Sample the moon texture to grab a more accurate color. (May not work correctly when not using the moon overhaul.)
        moon = Lighting(MoonPosition, screenCoords, coords, MoonColor * tex2D(Moon, .5), cloud);
    
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