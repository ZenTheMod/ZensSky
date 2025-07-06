sampler Clouds : register(s0);
sampler Moon : register(s1);

float2 ScreenSize;

float2 Pixel;

float2 Flipped;

bool UseEdgeLighting;

bool DrawSun;
float2 SunPosition;
float4 SunColor;

bool DrawMoon;
float2 MoonPosition;
float4 MoonColor;

float EdgeLighting(float2 screen, float2 light, float2 uv)
{
    float3 e = float3(Pixel, 0);
    
    float up = tex2D(Clouds, uv - e.zy).a * step(0, uv.y - e.y);
    float down = tex2D(Clouds, uv + e.zy).a * (1 - step(1, uv.y + e.y));
    float right = tex2D(Clouds, uv - e.xz).a * step(0, uv.x - e.x);
    float left = tex2D(Clouds, uv + e.xz).a * (1 - step(1, uv.x + e.x));
    
    float dy = (up - down) * .5;
    float dx = (right - left) * .5;
    
    float2 direction = float2(dx, dy) * Flipped;
    
        // Get the dot product between the alpha difference angle and the direction to the sun.
    float dirsun = dot(direction, normalize(light - screen));
    
    return saturate(dirsun);
}

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
    
    float4 outer = float4(0, 0, 0, 0);
    
    if (UseEdgeLighting)
    {
        float edge = EdgeLighting(screen, light, uv);
        
        float distedge = saturate(1 - (dist * 2.5));
        
        outer = lightColor * 1.5 * distedge * edge;
    }
    
    float4 inner = lightColor * 2. * distlight * glow;
    
    return saturate(inner) + saturate(outer);
}

    // https://www.shadertoy.com/view/tX2Xzc
        // This shader uses SV_POSITION to grab the coordinate on the screen of the pixel being drawn, rather than the texture coordinate.
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