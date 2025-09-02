#ifndef COMMON_FX
#define COMMON_FX

float map(float value, float start1, float stop1, float start2, float stop2)
{
    return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
}
float clampedMap(float value, float start1, float stop1, float start2, float stop2)
{
    value = clamp(value, start1, stop1);
    return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
}

float2x2 rotationMatrix(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2x2(float2(c, s), 
                    float2(-s, c));
}

float2 rotate(float2 coords, float2 center, float angle)
{
    float2 translatedCoords = coords - center;
    
    float2x2 rotationMat = rotationMatrix(angle);
    
    float2 rotatedCoords;
    rotatedCoords.x = dot(translatedCoords, rotationMat[0].xy);
    rotatedCoords.y = dot(translatedCoords, rotationMat[1].xy);
    
    return rotatedCoords;
}

float inCubic(float t)
{
    return pow(t, 3);
}
float outCubic(float t)
{
    return 1 - inCubic(1 - t);
}
float inOutCubic(float t)
{
    if (t < .5) 
        return inCubic(t * 2) * .5;
    return 1 - inCubic((1 - t) * 2) * .5;
}

    // https://bottosson.github.io/posts/oklab / https://www.shadertoy.com/view/ttcyRS
static const float3x3 kCONEtoLMS = float3x3(
         .4121656120, .2118591070, .0883097947,
         .5362752080, .6807189584, .2818474174,
         .0514575653, .1074065790, .6302613616);
    
static const float3x3 kLMStoCONE = float3x3(
         4.0767245293, -1.2681437731, -.0041119885,
        -3.3072168827, 2.6093323231, -.7034763098,
         .2307590544, -.3411344290, 1.7068625689);

float3 toOklab(float3 rgb)
{
    return pow(mul(kCONEtoLMS, rgb), .33333);
}

float3 toRGB(float3 oklab)
{
    return mul(kLMStoCONE, pow(oklab, 3));
}

float4 oklabLerp(float4 colA, float4 colB, float h)
{
    float3 lmsA = toOklab(colA.rgb);
    float3 lmsB = toOklab(colB.rgb);
    
    float3 lms = lerp(lmsA, lmsB, h);
    
    return float4(toRGB(lms), lerp(colA.a, colB.a, h));
}

    // https://www.shadertoy.com/view/4dKcWK
static const float EPSILON = 1e-10;

float3 HUEtoRGB(float hue)
{
        // Hue [0..1] to RGB [0..1]
        // See http://www.chilliant.com/rgb2hsv.html
    float3 rgb = abs(hue * 6. - float3(3, 2, 4)) * float3(1, -1, -1) + float3(-1, 2, 2);
    return saturate(rgb);
}

float3 RGBtoHCV(float3 rgb)
{
        // RGB [0..1] to Hue-Chroma-Value [0..1]
        // Based on work by Sam Hocevar and Emil Persson
    float4 p = (rgb.g < rgb.b) ? float4(rgb.bg, -1, .666) : float4(rgb.gb, 0, -.333);
    float4 q = (rgb.r < p.x) ? float4(p.xyw, rgb.r) : float4(rgb.r, p.yzx);
    float c = q.x - min(q.w, q.y);
    float h = abs((q.w - q.y) / (6 * c + EPSILON) + q.z);
    return float3(h, c, q.x);
}

float3 HSLtoRGB(float3 hsl)
{
        // Hue-Saturation-Lightness [0..1] to RGB [0..1]
    float3 rgb = HUEtoRGB(hsl.x);
    float c = (1 - abs(2 * hsl.z - 1)) * hsl.y;
    return (rgb - .5) * c + hsl.z;
}

float3 RGBtoHSL(float3 rgb)
{
        // RGB [0..1] to Hue-Saturation-Lightness [0..1]
    float3 hcv = RGBtoHCV(rgb);
    float z = hcv.z - hcv.y * 0.5;
    float s = hcv.y / (1 - abs(z * 2 - 1) + EPSILON);
    return float3(hcv.x, s, z);
}

    // https://www.shadertoy.com/view/M3dXzB
static const float2x2 coronariesMatrix = float2x2(cos(1 + float4(0, 33, 11, 0)));

float coronaries(float2 uv, float time)
{
    float2 a = float2(0, 0);
    float2 res = float2(0, 0);
    float s = 12;
    
    for (float j = 0; j < 12; j++)
    {
        uv = mul(uv, coronariesMatrix);
        a = mul(a, coronariesMatrix);
        
        float2 L = uv * s + j + a - time;
        a += cos(L);
        
        res += (.5 + .5 * sin(L)) / s;
        
        s *= 1.2;
    }
    
    return res.x + res.y;
}

static const float TAU = 6.28318530718;
static const float PI = 3.14159265359;
static const float PIOVER2 = 1.57079632679;

#endif