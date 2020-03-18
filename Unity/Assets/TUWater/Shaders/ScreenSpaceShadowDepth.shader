Shader "TU/ScreenSpaceShadowDepth"
{

	HLSLINCLUDE	
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;
		float3 _PlanetCenter;//the world-space position of the center of the planet
		float3 _LightUp;
		float3 _LightLeft;
		float3 _LightForward;
		float3 _LightDirection;
		
		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(_ShadowMap, sampler_ShadowMap);
		TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
		float4 _MainTex_TexelSize;

		inline float3 viewRayForUV(float3 l1, float3 l2, float3 r1, float3 r2, float2 uv)
		{
			return lerp(lerp(l2, l1, uv.y), lerp(r2, r1, uv.y), uv.x);
		}

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

		float4 frag1(vertout i) : SV_TARGET
		{
			//we'll need this regardless of anything else that happens
			float4 backgroundColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

			//return float4(normalize(i.view_ray),1);
			float3 camera = _WorldSpaceCameraPos;
			// Normalized view direction vector.
			float3 view_direction = normalize(i.view_ray);
			//0-1 linear depth value; 0= no depth, 1 = max depth (far clip)
			//0 should be near clip plane, but it appears to actually be '0'
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv));
			if(depth>=1)
			{
				return backgroundColor;
			}
			//distance from camera to the hit
			float distanceToDepthHit = depth * length(i.view_ray);

			float3 depthHit = camera + depth * i.view_ray;
			float3 objectHit = depthHit - _PlanetCenter;//distance from center of the object
			float3 sphereHit = normalize(objectHit);

			float3 lightHit = float3(dot(sphereHit, _LightLeft), dot(sphereHit, _LightUp), dot(sphereHit, _LightForward)); //no clue; where does this correction factor come from?
			lightHit = lightHit * length(objectHit);//normalized vector to camera space position
			lightHit = lightHit / 6700;//denormalized into -1 to +1 range
			float2 lightXY = ((lightHit + 1)/2).xy;
			lightXY.x = 1 - lightXY.x;
			
			float4 shadow = SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, lightXY);

			return float4(shadow.rrr, 1);
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

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag1
			ENDHLSL
		}

	}

}
