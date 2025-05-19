#include "../common.fx"

sampler planet : register(s0);

float4 shadowColor;

float planetRotation;
float shadowRotation;
float falloffStart;

const float pi = 3.14159;

const float shadowStart = .22;
const float shadowEnd = .28;

float2 getAngle(float2 d)
{
    float scaleY = sqrt(1 - d.y * d.y);
    
    float angY = .5 + asin(d.y) / pi;
    float angX = (.5 + asin(d.x / scaleY) / pi) * .5;
    
    return float2(angX, angY);
}

float shadow(float x)
{
    float shad = clampedMap(abs(frac(x) - .5), shadowStart, shadowEnd, 0., 1.);
    
    return shad * shad * (3.0 - 2.0 * shad);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
        // First grab the distance from the center.
    float2 distance = (coords - .5) * 2;
    float2 angle = getAngle(distance);
    
        // Then create our angles.
    float textureAngX = angle.x + planetRotation;
    float shadowAngX = angle.x + shadowRotation;
    
        // Sample the planet texture.
    float4 col = tex2D(planet, float2(textureAngX, angle.y));
    
        // Generate the shadow.
    float shadowInterpolator = shadow(shadowAngX);
    col = lerp(col * sampleColor, shadowColor, shadowInterpolator);
    
        // Add subtle falloff.
    float distanceSqr = distance.x * distance.x + distance.y * distance.y;
    col *= clampedMap(distanceSqr, falloffStart, 1, 1, 0); // falloff
    return col;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}