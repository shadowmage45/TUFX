Shader "TU/Water"
{

	HLSLINCLUDE	
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;

		//scene setup parameters -- location and size of the planet, direction of the light source
		float3 _PlanetCenter;//the world-space position of the center of the planet
		float _Radius;//radius of the body; this is the 'sea level' value
		float3 _SunDirection;//normalized direction of light source
		float3 _LightColor;//light color and intensity

		//effect setup parameters

		float _MaxWaveHeight;//maximum displacement from sea level
		float _RefractionStrength;
		float _R0;//index of refraction, used in fresnel calculations

		float _TransitionFactor;//transition factor for shorlines
		
		float3 _SurfaceColor;//color of the water surface
		float3 _DepthColor;//no clue how/why this is used...
		float3 _Extinction;//Meter depth at which RGB light from background objects is reduced

		

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
			_R0 = 0.5;
			float angle = 1.0f - saturate(dot(normal, eyeVec));
			float fresnel = angle * angle;
			fresnel = fresnel * fresnel;
			fresnel = fresnel * angle;
			return saturate(fresnel * (1.0f - saturate(_R0)) + _R0 - _RefractionStrength);
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
			if ( distanceToDepthHit > distanceToSeaExit && distanceToSeaLevel==0 )
			{
				//return float4(1,0,0,1);
				//return backgroundColor;
			}
			
			distanceToDepthHit = min(distanceToSeaExit, distanceToDepthHit);

			float3 worldPos = camera + (distanceToSeaLevel * view_direction);
			float3 normal = normalize(worldPos - _PlanetCenter);
			float3 lightColor = float3(1, 0.95, 0.83);

			//distort the surface normal by using the world position as inputs to distort tangent-space normal x/y			
			float3 distort = float3(frac(worldPos.x*0.05)*0.25, frac(worldPos.y*0.05)*0.25, frac(worldPos.z*0.05)*0.25);
			float dotdn = dot(distort, normal);
			//float3 d2 = float3(distort.r 
			//normal = normalize(abs(distort)*0.5 + normal);

			//depth in (scaled) meters that the ray traverses through the medium
			float oceanDepth = distanceToDepthHit - distanceToSeaLevel;

			//extinction depths (meters):
			//red = 4.5
			//orange = 15
			//yellow = 30
			//violet = 30
			//green = 75
			//blue = 300
			
			//oceanDepth /= 100;
			float eRed = 4.5;
			float eGreen = 75;
			float eBlue = 300;

			float3 extinctionFactor = saturate(float3(4.5, 75, 300) / (oceanDepth));
			//return float4(backgroundColor.rgb * extinctionFactor, 1);

			float3 oceanColor = float3(0.5, 0.5, 1);
			

			
			//standard blinn-phong lighting model
			//diffuse light intensity, from surface normal and light direction
			float diff = max (0, dot (normal, _SunDirection));
			
			//specular light calculations for the surface
			float3 h = normalize (_SunDirection - view_direction);
			float nh = max (0, dot (normal, h));
			float angle = 1;//what dimension is this?
			float spec = pow (nh, angle * 128);

			//exponential depth fog
			float scalar = 0.001;
			scalar = exp2(-scalar * oceanDepth);

			float3 light = diff * lightColor + spec.rrr * lightColor;
			float3 light1 = lerp(float3(0,0,0), light, scalar);
			

			//float3 color = lerp(oceanColor * light, backgroundColor * extinctionFactor, scalar);
			float3 color = backgroundColor.rgb * extinctionFactor;
			float fresnel = fresnelTerm(normal, -view_direction);
			//return float4(fresnel.rrr, 1);

			half3 specular = 0.0f;
			float shininess = 5;
			half3 mirrorEye = (2.0f * dot(view_direction, normal) * normal - view_direction);
			half dotSpec = saturate(dot(mirrorEye.xyz, -_SunDirection) * 0.5f + 0.5f);

			specular = (1.0f - fresnel) * ((pow(dotSpec, 512.0f)) * (shininess * 1.8f + 0.2f))* lightColor;
			specular += specular * 25 * saturate(1 - 0.05f) * lightColor;
			float3 reflect = float3(0.4, 0.7, 1) * diff;
			
			color = lerp(color, reflect, fresnel);
			color = saturate(color + spec);
						
			//backgroundColor.rgb = saturate(backgroundColor.rgb * (1 - color.bbb));
			//color += spec.rrr*10;

			return float4(saturate(color), 1);
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
