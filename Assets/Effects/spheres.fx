#ifndef SPHERES_FX
#define SPHERES_FX

#include "common.fx"

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

float3 sphere(float2 uv, float dist, float radius)
{
    float z = radius * sin(acos(dist / radius));
    
        // Calculate the sphere normals from -1 to 1.
    float3 sp = float3(uv, z);
    
    float3 sphererot = mul(sp, mul(mul(rotateX(-PIOVER2), rotateY(PI)), rotateZ(PIOVER2)));
    
    return sphererot;
}

float shadow(float3 sp, float shadowRotation)
{
    float shad = outCubic(dot(sp, mul(float3(0, 1, 0), rotateZ(TAU - PIOVER2 + shadowRotation))));
    
    shad = saturate(shad);
    
    return shad;
}

float4 atmo(float dist, float shad, float radius, float4 atmosphereColor, float4 atmosphereShadowColor)
{
        // Hacky solution for a faux atmosphere.
    float atmo = inCubic(1 - clampedMap(dist, radius, 1, 0, 1));
		
    float4 atmoColor = lerp(atmosphereShadowColor, atmosphereColor, shad);
		
    return atmoColor * atmo;
}

#endif