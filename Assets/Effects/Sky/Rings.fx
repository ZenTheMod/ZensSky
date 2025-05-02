sampler uImage0 : register(s0);

float uAngle;
float4 ShadowColor;
float ShadowExponent;
float ShadowSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 center = float2(0.5, 0.5);
    float2 translatedCoords = coords - center;
    
    float2x2 rotationMatrix = float2x2(cos(uAngle), -sin(uAngle),
                                       sin(uAngle), cos(uAngle));
    
    float2 rotatedCoords;
    rotatedCoords.x = dot(translatedCoords, rotationMatrix[0].xy);
    rotatedCoords.y = dot(translatedCoords, rotationMatrix[1].xy);
    
    float4 rings = tex2D(uImage0, saturate(rotatedCoords + center));
    
    if (rotatedCoords.y > 0.)
        return rings;
    
    float shadowInterpolator = clamp(abs(rotatedCoords.x * ShadowSize), 0., 1.);
    shadowInterpolator = 1. - pow(2., ShadowExponent * (shadowInterpolator - 1.));

    return lerp(rings, ShadowColor * rings.a, shadowInterpolator);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
