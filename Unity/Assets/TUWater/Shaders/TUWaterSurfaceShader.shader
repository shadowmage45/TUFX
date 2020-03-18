Shader "TU/WaterSurface"
{

	HLSLINCLUDE	
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;
		float3 _Up;

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
		float4 _MainTex_TexelSize;

		#include "Functions.hlsl"
		#include "randomNoise2.cginc"

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
			o.view_ray = viewRayForUV(_Left, _Left2, _Right, _Right2, o.uv);

			return o;
		}

		float3 sphereTangentToWorld(float3 normal, float3 tangentNormal)
		{
			float3 left = cross(float3(0, 1, 0), normal);
			float3 up = cross(left, normal);

			float x = dot(left, tangentNormal);
			float y = dot(up, tangentNormal);
			float z = dot(normal, tangentNormal);
			return float3(x,y,z);
		}

		float3 viewRayForUV(float2 uv)
		{
			return viewRayForUV(_Left, _Left2, _Right, _Right2, uv);
		}

		float2 getAdjustedUV(float3 mainRay, float2 uv, float horizontal, float vertical)
		{
			float2 tex = _MainTex_TexelSize;//rotateTexel(mainRay, _MainTex_TexelSize);
			uv.x += horizontal * tex.x;
			uv.y += vertical * tex.y;
			return uv;
		}

		float noise3(float3 pos)
		{
			float scaleFactor = _Radius*0.1;
			float noise = 0;
			noise +=  snoise(normalize(pos) * scaleFactor * 8.00) * 0.015;
			noise +=  snoise(normalize(pos) * scaleFactor * 4.00) * 0.015;
			noise += snoise(normalize(pos) * scaleFactor * 2.00) * 0.025;
			noise += snoise(normalize(pos) * scaleFactor * 1.00) * 0.050;
			noise += snoise(normalize(pos) * scaleFactor * 0.50) * 0.100;
			noise += snoise(normalize(pos) * scaleFactor * 0.25) * 0.300;
			noise += snoise(normalize(pos) * scaleFactor * 0.05) * 0.500;	
			noise = snoise(normalize(pos) * scaleFactor * 0.05);		
			return ((noise + 1) * 0.5) * 0.5;
		}

		float sampleNoiseView(float3 viewRay, float2 uv, out float3 normal, out float3 pos)
		{
			
			float3 camera = _WorldSpaceCameraPos;
			float3 p = camera - _PlanetCenter;
			float p_dot_v = dot(p, normalize(viewRay));
			float p_dot_p = dot(p, p);
			float ray_earth_center_squared_distance = p_dot_p - p_dot_v * p_dot_v;
			float AT = sqrt(_Radius * _Radius - ray_earth_center_squared_distance);
			float distanceToSeaLevel = -p_dot_v - AT;
			float distanceToSeaExit = -p_dot_v + AT;
			pos = camera;

			if (dot(p,p) > _Radius * _Radius && p_dot_v > 0)
			{
				return 0;
			}

			if(ray_earth_center_squared_distance > _Radius * _Radius )
			{
				return 0;
			}

			//0-1 linear depth value; 0= no depth, 1 = max depth (far clip)
			//0 should be near clip plane, but it appears to actually be '0'
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
			//distance from camera to the hit
			float distanceToDepthHit = depth * length(viewRay);

			if ( depth >= 1)
			{
				distanceToDepthHit = distanceToSeaExit;
			}

			//inside water; ray start at 0
			if(dot(p,p) < _Radius * _Radius)
			{
				distanceToSeaLevel = 0;
			}

			//the basic test...
			if ( distanceToSeaLevel > distanceToDepthHit)
			{
				return 0;
			}

			float3 startPos = camera + (distanceToSeaLevel * normalize(viewRay));
			pos = startPos - _PlanetCenter;

			normal = normalize(pos);
			return noise3(pos);
		}

		float sampleNoise(float2 uvStart, out float3 normal)
		{
			float3 mainRay = viewRayForUV(uvStart);
			normal = float3(0,0,0);
			//UVs for the four adjacent texels (currently in screen-space offsets)
			//TODO these UVs need to be rotated by the camera->world matrix, so the ray doing the sampling
			//is the left/right/up/down of the pixel -in world space-
			float2 uvL = getAdjustedUV(mainRay, uvStart, -1, 0);
			float2 uvR = getAdjustedUV(mainRay, uvStart, 1, 0);
			float2 uvU = getAdjustedUV(mainRay, uvStart, 0, 1);
			float2 uvD = getAdjustedUV(mainRay, uvStart, 0, -1);

			float3 n1 = float3(0.0, 0.0, 0.0);
			float3 pos = float3(0,0,0);
			float nO = sampleNoiseView(mainRay, uvStart, normal, pos);
			float dist = length(pos);//distance to hit
			float texAngle = 90 / 1920;//horizontal FOV, degrees per pixel
			float texRad = texAngle * (PI / 180);
			float dist2 = sqrt(dist * dist + dist * dist - (2 * dist * dist * cos(texRad)));//it should be this, but it is unstable?
			//world-space 'width' of a pixel at distance //need to know... aspect ratio?
			float texSize = 90/dist;//this is wrong, but should work for approximation
			
			float2 longlat = cartesianToSpherical(pos);
			float xSkew = 1 / cos(longlat.y + PI * 0.5);
			
			float nL = noise3(sphericalToCartesian(float2(longlat.x - texSize * xSkew, longlat.y)));
			float nR = noise3(sphericalToCartesian(float2(longlat.x + texSize * xSkew, longlat.y)));
			float nU = noise3(sphericalToCartesian(float2(longlat.x, longlat.y + texSize)));
			float nD = noise3(sphericalToCartesian(float2(longlat.x, longlat.y - texSize)));

			//TODO return to this
			// the intent is to derive a tangent the same way you would for three vertices
			// but working backwards from UV coordinates
			float3 npos = normalize(pos);
			float2 uv = cartesianToUV(npos);
			float3 v3 = UVToCartesian(float2(uv.x + 1/360, uv.y));
			float lx = length(npos-v3);
			float3 v2 = UVToCartesian(float2(uv.x, uv.y + 1/360));
			float ly = length(npos-v2);
			float3 rPos = UVToCartesian(uv);

			//tangent and bitangents
			float3 t = normalize(npos - v3);
			float3 b = normalize(npos - v2);
			b = normalize(cross(normal, t));

			nL = noise3(npos + t*lx);
			nR = noise3(npos - t*lx);
			nU = noise3(npos + b*ly);
			nD = noise3(npos - b*ly);

			//http://corysimon.github.io/articles/uniformdistn-on-sphere/
			//https://math.libretexts.org/Bookshelves/Calculus/Book%3A_Calculus_(OpenStax)/12%3A_Vectors_in_Space/12.7%3A_Cylindrical_and_Spherical_Coordinates
			//convert from cartesian to spherical coordinates
			//use equirectangular projection to skew the X increment
			//add skewed(X),Y
			//convert back to cartesian

			float tangentSpaceNormal = normalize(float3(2*(nR - nL), 2*(nU - nD), 4));
			normal = float3(dot(tangentSpaceNormal, t), dot(tangentSpaceNormal, b), dot(tangentSpaceNormal, normal))+normal;
			normal = normalize(normal);
			//normal = (1 + -normal) * 0.5;
			return (nO + 1) * 0.5;
		}

		float4 frag1(vertout i) : SV_TARGET
		{
			//we'll need this regardless of anything else that happens
			float4 backgroundColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

			//return float4(normalize(i.view_ray),1);
			float3 camera = _WorldSpaceCameraPos;
			// Normalized view direction vector.
			float3 view_direction = normalize(i.view_ray);
			float3 normal;
			float ns = sampleNoise(i.uv, normal);
			//if(ns==0){return backgroundColor;}
			
			return float4(abs(normal.rgb), 1);
			//return float4(ns.rrr, 1);
			
						//specular light calculations for the surface
			float3 h = normalize (_SunDirection - view_direction);
			float nh = max (0, dot (normal, h));			
			float spec = pow (nh, _S0 * 128) * _S1;
			float diff = max (0, dot (normal, _SunDirection));
			return float4(ns, 0, spec,  1);
			return float4(backgroundColor.rgb * diff + spec.rrr, 1);

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

			//0-1 linear depth value; 0= no depth, 1 = max depth (far clip)
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

			//the world-space position of the 'distanceToSeaLevel' coordinate
			float3 worldPos = camera + (distanceToDepthHit * view_direction);
			//surface normal of the ocean
			normal = normalize((camera + view_direction * distanceToSeaLevel)  - _PlanetCenter);
			//return float4(abs(normal),1);
			float3 startPos = camera + (distanceToSeaLevel * view_direction);

			startPos -= _PlanetCenter;

			float2 longlat = cartesianToSpherical(startPos);
			//return float4(longlat.x * 0, longlat.y, 0, 1);
			//return float4(frac((longlat / (2 * PI)) * 3), 0, 1);
			//startPos = sphericalToCartesian(longlat);
			//return float4(frac(normalize(startPos)*10), 1);
			float skewedX = longlat.x * cos(longlat.y + PI * 0.5);
			if(skewedX < 0.1 && skewedX > -0.1)
			{
				return float4(1,0,0,1);
			}

			float noise = snoise(longlat * 1);
			float scaleFactor = 100;
			noise = snoise(normalize(startPos) * scaleFactor  * 1.00) * 0.4;
			noise += snoise(normalize(startPos) * scaleFactor * 0.50) * 0.3;
			noise += snoise(normalize(startPos) * scaleFactor * 0.25) * 0.2;
			noise += snoise(normalize(startPos) * scaleFactor * 0.05) * 0.1;
			return float4(noise.rrr, 1);

			//this is the depth that the view ray travels through the ocean before it hits the 'point' (or exits from the surface)			
			float viewOceanDepth = distanceToDepthHit - distanceToSeaLevel;
			//this is the depth that the sun ray must travel through the ocean to impact the surface that was hit
			float sunOceanDepth = sphereExitDistance(worldPos, _PlanetCenter, _SunDirection, _Radius);

			return float4(backgroundColor);
		}

		float4 frag2(vertout i) : SV_TARGET
		{
			//we'll need this regardless of anything else that happens
			float4 backgroundColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);


			return float4(backgroundColor);
		}

	ENDHLSL

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		//first pass write out surface normal (rgb) and noise (a)
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag1
			ENDHLSL
		}

		//second pass 
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag2
			ENDHLSL
		}

	}

}
