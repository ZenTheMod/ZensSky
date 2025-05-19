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

    // https://bottosson.github.io/posts/oklab / https://www.shadertoy.com/view/ttcyRS
const float3x3 kCONEtoLMS = float3x3(
         0.4121656120, 0.2118591070, 0.0883097947,
         0.5362752080, 0.6807189584, 0.2818474174,
         0.0514575653, 0.1074065790, 0.6302613616);
    
const float3x3 kLMStoCONE = float3x3(
         4.0767245293, -1.2681437731, -0.0041119885,
        -3.3072168827, 2.6093323231, -0.7034763098,
         0.2307590544, -0.3411344290, 1.7068625689);

float4 oklabLerp(float4 colA, float4 colB, float h)
{
    float3 lmsA = pow(mul(kCONEtoLMS, colA.rgb), 0.33333);
    float3 lmsB = pow(mul(kCONEtoLMS, colB.rgb), 0.33333);
    
    float3 lms = lerp(lmsA, lmsB, h);
    
    return float4(mul(kLMStoCONE, (lms * lms * lms)), lerp(colA.a, colB.a, h));
}

#endif