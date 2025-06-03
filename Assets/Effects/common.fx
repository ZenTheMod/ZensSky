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
const float3x3 kCONEtoLMS = float3x3(
         0.4121656120, 0.2118591070, 0.0883097947,
         0.5362752080, 0.6807189584, 0.2818474174,
         0.0514575653, 0.1074065790, 0.6302613616);
    
const float3x3 kLMStoCONE = float3x3(
         4.0767245293, -1.2681437731, -0.0041119885,
        -3.3072168827, 2.6093323231, -0.7034763098,
         0.2307590544, -0.3411344290, 1.7068625689);

float3 toOklab(float3 rgb)
{
    return pow(mul(kCONEtoLMS, rgb), 0.33333);
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

    // https://www.shadertoy.com/view/M3dXzB
const float2x2 coronariesMatrix = float2x2(cos(1 + float4(0, 33, 11, 0)));

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

const float TAU = 6.28318530718;
const float PI = 3.14159265359;
const float PIOVER2 = 1.57079632679;

float aafi(float2 p)
{
    float fi = atan2(p.y, p.x);
    fi += step(p.y, 0) * TAU;
    return fi;
}

float2 lonlat(float3 p)
{
    float lon = aafi(p.xy) / TAU;
    float lat = aafi(float2(p.z, length(p.xy))) / PI;
    return float2(1 - lon, lat);
}

float3x3 rotateX(float f)
{
    return float3x3(
	    float3(1, 0, 0),
	    float3(0, cos(f), -sin(f)),
		float3(0, sin(f), cos(f))
    );
}

float3x3 rotateY(float f)
{
    return float3x3(
	    float3(cos(f), 0, sin(f)),
	    float3(0, 1, 0),
		float3(-sin(f), 0, cos(f))
    );
}

float3x3 rotateZ(float f)
{
    return float3x3(
	    float3(cos(f), -sin(f), 0),
	    float3(sin(f), cos(f), 0),
		float3(0, 0, 1)
    );
}

#endif