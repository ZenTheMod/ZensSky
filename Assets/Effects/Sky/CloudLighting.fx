sampler Clouds : register(s0);

float2 ScreenSize;

float2 SunPosition;
float4 SunColor;

bool UseEdgeDetection;

const int Pixel = 28;

float s(float2 offset, float2 coords)
{
    return tex2D(Clouds, coords + offset).a;
}

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
    float glow = distsun * ((shadows * 3) + (clouds.r * 0.8));
    
    float4 color = clouds * sampleColor;
    
        // I personally dislike how it looks.
    if (UseEdgeDetection)
    {
        float3 e = float3(Pixel / ScreenSize, 0);
        
            // Sample neighbors and compare the alpha.
        float up = s(-e.zy, coords);
        float down = s(e.zy, coords);
        float left = s(-e.xz, coords);
        float right = s(e.xz, coords);
    
        float dy = (up - down) * .5;
        float dx = (right - left) * .5;
    
            // Get a direction.
        float direction = float2(dx, dy);
            // Get the dot product between the alpha difference angle and the direction to the sun.
        float dirsun = dot(direction, normalize(sun - screen)) * 3;
        
        glow += max(dirsun, 0);
    }
    
        // Mashy Mashy.
            // return color + (SunColor * sampleColor.a * distsun * (max(dirsun, 0) + glow) * center.a);
    return color + (SunColor * clouds.a * max(color.a, 0.7) * distsun * glow);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}