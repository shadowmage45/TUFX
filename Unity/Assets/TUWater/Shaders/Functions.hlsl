/*

	Helper functions used by the TU post process shaders.

*/
inline float3 viewRayForUV(float3 l1, float3 l2, float3 r1, float3 r2, float2 uv)
{
	return lerp(lerp(l2, l1, uv.y), lerp(r2, r1, uv.y), uv.x);
}

float2 cartesianToSpherical(float3 worldPos)
{
	worldPos = normalize(worldPos);
	//float long = (PI + atan2(worldPos.z, worldPos.x)) / (2*PI);
	//float lat = abs((acos(worldPos.y) / (PI)) * 2 - 1);
	float long = atan2(worldPos.z, worldPos.x);
	float lat = acos(worldPos.y);
	return float2(long, lat);
}

float3 sphericalToCartesian(float2 spherePos)
{
	float x = sin(spherePos.y) * cos(spherePos.x);
	float y = -cos(spherePos.y);
	float z = sin(spherePos.y) * sin(spherePos.x);
	return float3(x, y, z);
}

float2 sphericalToEquirectangular(float2 spherical)
{
	float x = 1 * spherical.x * cos(spherical.y);
	float y = 1 * spherical.y;
	return float2(x,y);
}

//Input is a sphere-centric surface world-position
//Output is the UV coordinate of that position, assuming equirectangular UV mapping
float2 cartesianToUV(float3 worldPos)
{
	//https://en.wikipedia.org/wiki/UV_mapping
	//http://paulbourke.net/geometry/transformationprojection/
	worldPos = normalize(worldPos);
	float u = 0.5 + (atan2(worldPos.z, worldPos.x) / (2 * PI));
	float v = 0.5 - (asin(worldPos.y) / PI);
	return float2(u, v);
}

float3 UVToCartesian(float2 uv)
{
	float theta = 2 * PI * (uv.x);
	float phi = PI * (uv.y);
	float x = cos(theta) * sin(phi);
	float y = -cos(phi);
	float z = sin(theta) * sin(phi);
	return float3(x,y,z);
}

float2 UVToSpherical(float2 uv)
{
	uv.x *= 2 * PI;
	return uv;
}

// Function calculating fresnel term.
// - normal - normalized normal vector
// - eyeVec - normalized eye vector
float fresnelTerm(float3 normal, float3 eyeVec, float R0, float R2)
{
	float angle = 1.0f - saturate(dot(normal, eyeVec));
	float fresnel = angle * angle;
	fresnel = fresnel * fresnel;
	fresnel = fresnel * angle;
	return saturate(fresnel * (1.0f - saturate(_R0)) + R0 - R2);
}

// Returns a three-vector tangent space matrix for the input normal, world-position, and uv coordinate
float3x3 tangentFrame(float3 normal, float3 P, float2 uv)
{
	//because, apparently DDX and DDY do... some interesting semi-async stuff;
	//they return the derivative of the input expression(value) for the currently running pixel patch
	//so this is actually a value dependent upon the values of other nearby pixels (woof)
			
	float3 dp1 = ddx(P);
	float3 dp2 = ddy(P);
	float2 duv1 = ddx(uv);
	float2 duv2 = ddy(uv);
	
	float3x3 M = float3x3(dp1, dp2, cross(dp1, dp2));
	float2x3 inverseM = float2x3( cross( M[1], M[2] ), cross( M[2], M[0] ) );
	float3 T = mul(float2(duv1.x, duv2.x), inverseM);
	float3 B = mul(float2(duv1.y, duv2.y), inverseM);
	return float3x3(normalize(T), normalize(B), normal);
}

//Returns the two intersects of the view direction with the sphere, or false if there are no intersects
bool raySphereIntersect(float3 origin, float3 center, float3 direction, float radius, out float entry, out float exit)
{
	entry = exit = 0;
	float t0, t1; // solutions for t if the ray intersects 
	// geometric solution
	float3 L = center - origin; 
	float tca = dot(L, direction);
	//ray does not intersect sphere in view direction
	if (tca < 0) 
	{
		return false;
	}			
	float d2 = dot(L, L) - tca * tca;
	//ray is entirely outside of the sphere
	if (d2 > _Radius * _Radius)
	{
		return false; 
	}
			
	float thc = sqrt(_Radius * _Radius - d2); 
	t0 = tca - thc; 
	t1 = tca + thc;
	//both intersects are behind the ray
	if(t0< 0 && t1 < 0)
	{
		return false;
	}
	entry = t0;
	exit = t1;

	if (t0 > t1)
	{
		float tt = t0;
		//t0 = t1;
		//t1 = tt;
	}
 
	if (t0 < 0) 
	{ 
		//t0 = t1; // if t0 is negative, let's use t1 instead 
		if (t0 < 0)
		{
			//return false; // both t0 and t1 are negative 
		} 
	}
	entry = t0;
	exit = t1;
 
	return true; 
}

//Calculates the distance a ray will travel to exit a sphere, when started at 'pos' (inside sphere)
// and cast along 'direction'
float sphereExitDistance(float3 pos, float3 center, float3 direction, float radius)
{
	float3 p = center - pos;			
	float tca = dot(p, direction);//distance along the ray to the shortest distance to sphere point
	float d2 = dot(p, p) - tca * tca;
	float thc = sqrt(radius * radius - d2);
	return tca + thc;
}

//integrate over the distance between origin and end, calculating optical depth at each segment
//
float3 getIrradiance(float3 origin, float3 end, float3 center, float radius)
{
	float3 ray = normalize(end - origin);
	int steps = 32;
	float dist = distance(origin, end);
	float step = dist / steps;
	float3 irradiance = float3(0,0,0);
	for(int i = 0; i < steps; i++)
	{
		float d1 = i * step;//distance along the ray being sampled
		float3 t = origin + ray * (d1);//point along the ray being sampled
		float depth = sphereExitDistance(t, center, normalize(t - center), radius);//depth below the surface of the point being sampled
		float3 color = pow(_Extinction * _LightColor, depth) * _WaterColor;//light extinction on the way to the sampled point
		irradiance += pow(_Extinction, d1) * step * color;
	}
	return irradiance * _Clarity;
}
