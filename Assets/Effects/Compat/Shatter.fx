#include "../spheres.fxh"

sampler tex : register(s0);

float2 screenSize;

matrix projection;

float shadowRotation;

float4 color;
float4 shadowColor;

float4 innerColor;

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 WorldPosition : COLOR0;
    float3 Normal : NORMAL0;
    float3 TextureCoordinates : TEXCOORD0;
};

VSOutput VertexShaderFunction(in VSInput input)
{
    VSOutput output = (VSOutput) 0;
    
    float4 pos = mul(input.Position, projection);
    output.Position = pos;
    
    output.WorldPosition = input.Position;
    
    output.Normal = input.Normal;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VSOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;
    float2 position = input.Position.xy;
    float2 screenCoords = position.xy / screenSize;
    float3 world = input.WorldPosition.xyz;
    
    float3 sp = input.Normal.xyz;
    
    float shad = outCubic(dot(sp, mul(float3(-1, 0, 0), rotateY(TAU - PIOVER2 + shadowRotation))));
    
    shad = saturate(shad);
    
    float inner = outCubic(dot(sp, -normalize(world)));
    
    inner = saturate(inner);
    
    float4 col = tex2D(tex, coords) * color;
    
    col = lerp(col, lerp(shadowColor, col * innerColor, inner), shad);
    
    return col * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}