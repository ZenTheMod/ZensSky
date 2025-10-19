#include "../spheres.fxh"

sampler Normals : register(s0);
sampler Body : register(s1);

float2 screenSize;

float rotation;

float2 lightPosition;

float4 lightColor;

bool useTexture;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float2 screenCoords : SV_POSITION, float4 sampleColor : COLOR0) : COLOR0
{
    float4 normal = tex2D(Normals, coords);
    
    float3 sp = normal.rgb - .5;
    sp *= 2;
    
    sp.rgb = mul(sp.rgb, rotateZ(rotation));
    
    float3 dir = normalize(float3((lightPosition - screenCoords) / screenSize, .0005));
    
    float4 light = lightColor * dot(sp.rgb, dir);
    
    if (useTexture)
        light *= tex2D(Body, .5);
    
    light *= normal.a;
    
    light.a = 0;
    
    return light;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}