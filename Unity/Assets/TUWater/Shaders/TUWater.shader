Shader "TU/Water"
{

	HLSLINCLUDE	
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;

		// _Time already is a value?
		float _Timer;//input time value, seed value for effects

		//scene setup parameters -- location and size of the planet, direction of the light source
		float3 _PlanetCenter;//the world-space position of the center of the planet
		float3 _SunCenter;//the world-space position of the center of the planet
		float _Radius;//radius of the body; this is the 'sea level' value
		float3 _SunDirection;//normalized direction of light source
		float3 _LightColor;//light color and intensity

		//effect setup parameters
		float _R0;//schlicks
		float _R2;//refraction strenght (scalar term)

		float _S0;//specular angle? (hardness?)
		float _S1;//specular strength (scalar)
		
		float _MaxDisplacement;//maximum displacement from sea level
		float _ShoreHardness;//transition factor for shorlines
		
		float3 _WaterColor;//color of the water surface / fog color
		float3 _Extinction;//ratio of color extinction
		float _Clarity;//speed of color extinction; how 'murky' the water is		

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

		struct at
		{
			float4 vertex : POSITION;
		};

		struct vertout
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			float3 view_ray : TEXCOORD1;
		};

		vertout vert(at v)
		{
			vertout o;
			o.vertex = float4(v.vertex.xy, 0.0, 1.0);
			o.uv = TransformTriangleVertexToUV(v.vertex.xy);
#if UNITY_UV_STARTS_AT_TOP
			o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif
			float3 left = lerp(_Left2, _Left, o.uv.y);
			float3 right = lerp(_Right2, _Right, o.uv.y);
			o.view_ray = lerp(left, right, o.uv.x);

			return o;
		}

		// Function calculating fresnel term.
		// - normal - normalized normal vector
		// - eyeVec - normalized eye vector
		float fresnelTerm(float3 normal, float3 eyeVec)
		{
			float angle = 1.0f - saturate(dot(normal, eyeVec));
			float fresnel = angle * angle;
			fresnel = fresnel * fresnel;
			fresnel = fresnel * angle;
			return saturate(fresnel * (1.0f - saturate(_R0)) + _R0 - _R2);
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

		float4 frag(vertout i) : SV_TARGET
		{
			//we'll need this regardless of anything else that happens
			float4 backgroundColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

			//return float4(normalize(i.view_ray),1);
			float3 camera = _WorldSpaceCameraPos;
			// Normalized view direction vector.
			float3 view_direction = normalize(i.view_ray);

			float3 p = camera - _PlanetCenter;
			float p_dot_v = dot(p, view_direction);
			float p_dot_p = dot(p, p);
			float ray_earth_center_squared_distance = p_dot_p - p_dot_v * p_dot_v;
			float AT = sqrt(_Radius * _Radius - ray_earth_center_squared_distance);
			float distanceToSeaLevel = -p_dot_v - AT;
			float distanceToSeaExit = -p_dot_v + AT;
			if (length(p) > _Radius && p_dot_v > 0)
			{
				//return float4(1,1,0,1);
				return backgroundColor;
			}

			//inside water; ray start at 0
			if(length(p) < _Radius)
			{
				distanceToSeaLevel = 0;
			}

			//0-1 linear depth value; 0= no depth, 1 = max depth
			//0 should be near clip plane, but it appears to actually be '0'
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv));
			//distance from camera to the hit
			float distanceToDepthHit = depth * length(i.view_ray);
			

			if(ray_earth_center_squared_distance > _Radius * _Radius )
			{
				//return float4(1,0,1,1);
				return backgroundColor;
			}

			if ( depth >= 1)
			{
				distanceToDepthHit = distanceToSeaExit;
			}

			//the basic test...
			if ( distanceToSeaLevel > distanceToDepthHit)
			{
				//return float4(0,1,1,1);
				return backgroundColor;
			}
			
			//the actual, final, end distance of the ray; this will either be the ocean surface (seen from below),
			//the ocean bottom (from above), or the depth value of some obstruction (above or below)
			//by this time it is guaranteed that the ray passes through at least some portion of water.
			distanceToDepthHit = min(distanceToSeaExit, distanceToDepthHit);

			//the world-space position of the 'distanceToDepthHit' coordinate //TODO -- adjust for underwater perspective
			float3 worldPos = camera + (distanceToDepthHit * view_direction);
			//surface normal of the ocean
			float3 normal = normalize((camera + view_direction * distanceToSeaLevel)  - _PlanetCenter);

			float3 startPos = camera + (distanceToSeaLevel * view_direction);


			//depth in (scaled) meters that the ray traverses through the medium

			//this is the depth that the view ray travels through the ocean before it hits the 'point' (or exits from the surface)			
			float viewOceanDepth = distanceToDepthHit - distanceToSeaLevel;
			//this is the depth that the sun ray must travel through the ocean to impact the surface that was hit
			float sunOceanDepth = sphereExitDistance(worldPos, _PlanetCenter, _SunDirection, _Radius);
			//the transmittance value of the light reaching the surface
			float3 sunRayTransmittance = pow(_Extinction, sunOceanDepth * _Clarity);
			backgroundColor.rgb = backgroundColor.rgb * sunRayTransmittance;

			//standard blinn-phong lighting model from legacy specular shaders...
			//diffuse light intensity, from surface normal and light direction
			float diff = max (0, dot (normal, _SunDirection));
			
			//specular light calculations for the surface
			float3 h = normalize (_SunDirection - view_direction);
			float nh = max (0, dot (normal, h));			
			float spec = pow (nh, _S0 * 128) * _S1;
			
			//return float4(getIrradiance(startPos, worldPos, _PlanetCenter, _Radius)*1.0*diff, 1);

			float fresnel = fresnelTerm(normal, -view_direction);
			//return float4(fresnel.rrr, 1);
			float3 reflectionVector = normalize(2 * dot(normal, -view_direction) * normal + view_direction);
			//return float4(reflectionVector, 1);

			//this does not deal with distortion/displacement
			float3 refraction = backgroundColor.rgb * diff;
			//transmittance for the background based purely on depth; this is what causes the bluish tint,
			//and occludes background objects as they get too far away
			float3 transmittance = pow(_Extinction, viewOceanDepth * _Clarity);
			//return float4(transmittance, 1);
			float3 irradiance = getIrradiance(startPos, worldPos, _PlanetCenter, _Radius);
			//return float4(irradiance, 1);
			refraction = saturate(refraction * transmittance + irradiance);
			float3 reflection = diff * float3(0.4, 0.4, 0.8);//sky reflection

			if(length(p) < _Radius)
			{
				fresnel = 0;//TODO - find a way to get reflection data...
			}			
			float3 wColor = _WaterColor;
			float3 color = lerp(refraction, reflection, fresnel);
			color = saturate(color + _LightColor * spec);
			//color = lerp(refraction, color, saturate(oceanDepth, _ShoreHardness));//TODO - only aerial perspective
			
						
			//backgroundColor.rgb = saturate(backgroundColor.rgb * (1 - color.bbb));
			//color += spec.rrr*10;

			return float4(color,1);
		}

	ENDHLSL

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			ENDHLSL
		}

	}

}
