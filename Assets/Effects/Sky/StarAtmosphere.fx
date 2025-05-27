sampler star : register(s0);
sampler atmosphere : register(s1);

float2 screenSize;
float2 sunPosition;
float distanceFadeoff;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float2 screenPosition : SV_POSITION, float4 sampleColor : COLOR0) : COLOR0
{
    float2 screenCoords = screenPosition / screenSize;
    
        // This is essentially just the realistic sky shader but without the twinkling.
    float distanceSqrFromSun = dot(screenPosition - sunPosition, screenPosition - sunPosition);
    float atmosphereInterpolant = dot(tex2D(atmosphere, screenCoords).rgb, 0.333);
    float opacity = saturate(1 - smoothstep(57600, 21500, distanceSqrFromSun / distanceFadeoff) - atmosphereInterpolant * 2.05);
    
    return tex2D(star, coords) * sampleColor * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
