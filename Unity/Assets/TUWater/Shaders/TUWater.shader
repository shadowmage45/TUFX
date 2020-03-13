Shader "TU/Water"
{

	HLSLINCLUDE	
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;
		float3 _PlanetCenter;
		float3 _SunDirection;
		float _Radius;

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

			//distort the surface normal by using the world position as inputs to distort tangent-space normal x/y			
			float3 distort = float3(frac(worldPos.x*0.05)*0.25, frac(worldPos.y*0.05)*0.25, frac(worldPos.z*0.05)*0.25);
			float dotdn = dot(distort, normal);
			//float3 d2 = float3(distort.r 
			//normal = normalize(abs(distort)*0.5 + normal);

			//depth in (scaled) meters
			float oceanDepth = distanceToDepthHit - distanceToSeaLevel;
			float3 oceanColor = float3(0.5, 0.5, 1);
			float3 lightColor = float3(1,0.95,0.83);

			
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
			

			float3 color = lerp(oceanColor * light, backgroundColor * light1, scalar);

						
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
