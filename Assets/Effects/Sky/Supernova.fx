#include "../common.fxh"
#include "../Compat/realisticSky.fxh"

sampler noise1 : register(s0);
sampler noise2 : register(s2);

    // 1 is reserved for Realistic Sky's atmosphere for compat.
sampler atmosphere : register(s1);

float expand;
float decay;

float4 explosionColor;
float hue;

float globalTime;

float2 offset;

static const float4 explosionStart = float4(2, 2, 2, 1);

float cloudMap(float2 uv, float dist)
{
    float2 off = tex2D(noise2, uv + (globalTime * .01)).rg;
	
    uv *= .33;
    
    float n = tex2D(noise1, offset + (uv / expand) + (float2(.09, .05) * off)).r * 1.55;
	
    dist *= 1.5;
	
    float falloff = 1 - pow(saturate(dist - (off.x * off.y)), 6);
	
    return n * falloff;
}

float4 nebula(float2 uv, float2 highlightDir, float dist)
{
    dist /= expand;
	
    float invDecay = 1 - decay;

    float n = cloudMap(uv, dist).r;

    float cloud = n * n * 2.1 * invDecay;
    float cloudOffset = n * cloudMap(uv + highlightDir, dist).r * 2.1 * invDecay;

    float cli = saturate(lerp(-.6, 1, .5 + 8 * ((cloud * 1.03) - (cloudOffset))) * (1 - (dist * 1.5)));
  
    float tc = saturate(cloud);
    float satc = lerp(.9, .5, tc);
  
    float3 colc = HSLtoRGB(float3(hue - pow(dist * n, 3.2) - (invDecay * .2), satc, .52)) + cli;
  
    tc *= tc;

    float3 col = colc * .66;
    col *= tc;
	
    col = pow(saturate(col), .4545);
	
    return float4(col, (col.r + col.g + col.b) * .333);
}

float4 explosion(float2 uv, float dist, float4 exploColor)
{
    float n = cloudMap(uv * expand, dist).r;
	
    float radius = outCubic(saturate(expand * .5)) + (n * n * expand) * (1 - dist) * expand;
	
    radius += (1 - dist) * expand * 4.8;
    radius *= 1 - pow(expand, 2);
    
    float4 explo = exploColor * (5 - inOutCubic(expand) * .7);
    
    return oklabLerp(0, explo, saturate(radius - dist - expand));
}

float4 supernova(float2 uv)
{
    float dist = length(uv) * 2;
    
    float4 exploColor = oklabLerp(explosionStart, explosionColor, expand);
    
    float4 expl = explosion(uv, dist, exploColor);
    
    float4 neb = nebula(uv, -normalize(uv) * .015, dist);
    
    neb *= inCubic(saturate(expand * 3));
    
    neb = oklabLerp(neb.a * exploColor, neb, expand);
    
    float4 col = neb + (expl * (1 - expand + neb.a));
    
    return col;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 screenPosition : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    coords -= .5;
    
    float4 color = supernova(coords);
    
    float2 screenCoords = screenPosition / screenSize;
    
    float opactity = 1;
    
    if (usesAtmosphere)
        opactity = StarOpacity(screenPosition, coords, sunPosition, tex2D(atmosphere, screenCoords).rgb, distanceFadeoff);
    
    color *= sampleColor * opactity;
    
    color.a = 0;
    
    color = saturate(color);
    
    return color;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}