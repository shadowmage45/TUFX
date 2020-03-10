Shader "TU/BIS"
{

	/**
	* Original copyright notice is below in its entirety.  Substantial portions
	* of this source have been modified to enable use as a post-process shader.
	*
	* HLSL conversion...
	*/

	/**
	* Copyright (c) 2017 Eric Bruneton
	* All rights reserved.
	*
	* Redistribution and use in source and binary forms, with or without
	* modification, are permitted provided that the following conditions
	* are met:
	* 1. Redistributions of source code must retain the above copyright
	*    notice, this list of conditions and the following disclaimer.
	* 2. Redistributions in binary form must reproduce the above copyright
	*    notice, this list of conditions and the following disclaimer in the
	*    documentation and/or other materials provided with the distribution.
	* 3. Neither the name of the copyright holders nor the names of its
	*    contributors may be used to endorse or promote products derived from
	*    this software without specific prior written permission.
	*
	* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
	* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
	* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
	* ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
	* LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
	* CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
	* SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
	* INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
	* CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
	* ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
	* THE POSSIBILITY OF SUCH DAMAGE.
	*/
	HLSLINCLUDE
		#define COMBINED_SCATTERING_TEXTURES 1
		//need this here, as it includes some field declarations, which apparently have to be done in this upper hlslinclude block
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
		#include "Definitions.hlsl"
		#include "UtilityFunctions.hlsl"
		#include "TransmittanceFunctions.hlsl"
		#include "ScatteringFunctions.hlsl"
		#include "IrradianceFunctions.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;

		//frustum bounding vectors for near clip, used to determine world-space view direction from inside screen space shader
		float3 _NLeft;
		float3 _NRight;
		float3 _NLeft2;
		float3 _NRight2;

		//farClip - nearClip - used to recreate world-space position from depth buffer
		//may or may not be correct...
		float _ClipDepth;

		float exposure;
		float3 white_point;
		float3 earth_center;
		float3 sun_direction;
		float2 sun_size;

		static const float3 kGroundAlbedo = float3(0.0, 0.0, 0.0);

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(transmittance_texture, sampler_transmittance_texture);
		TEXTURE2D_SAMPLER2D(irradiance_texture, sampler_irradiance_texture);
		TEXTURE3D_SAMPLER3D(scattering_texture, sampler_scattering_texture);
		TEXTURE3D_SAMPLER3D(single_mie_scattering_texture, sampler_single_mie_scattering_texture);
		TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

		#include "RenderingFunctions.hlsl"

		//appdata never has UV, what a terrible system..
		struct at
		{
			float4 vertex : POSITION;
		};

		struct vertout
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			float3 view_ray : TEXCOORD1;
			float3 view_ray2 : TEXCOORD2;
		};

		vertout vert(at v)
		{
			vertout o;
			o.vertex = float4(v.vertex.xy, 0.0, 1.0);
			//o.vertex = mul(mul(unity_MatrixVP, unity_ObjectToWorld), o.vertex);//apparently this is not correct for the triangle blit method (why the fuck are they doing this?)
			o.uv = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
			o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

			float3 left = lerp(_Left2, _Left, o.uv.y);
			float3 right = lerp(_Right2, _Right, o.uv.y);
			o.view_ray = lerp(left, right, o.uv.x);

			left = lerp(_NLeft2, _NLeft, o.uv.y);
			right = lerp(_NRight2, _NRight, o.uv.y);
			o.view_ray2 = lerp(left, right, o.uv.x);

			return o;
		}

		float4 frag(vertout i) : SV_TARGET
		{
			//return float4(normalize(i.view_ray),1);
			float3 camera = _WorldSpaceCameraPos;
			// Normalized view direction vector.
			float3 view_direction = normalize(i.view_ray);
			// Tangent of the angle subtended by this fragment.
			float fragment_angular_size = length(ddx(i.view_ray) + ddy(i.view_ray)) / length(i.view_ray);

			float shadow_in = 1;
			float shadow_out = 1;

			// Hack to fade out light shafts when the Sun is very close to the horizon.
			float lightshaft_fadein_hack = smoothstep(
				0.02, 0.04, dot(normalize(camera - earth_center), sun_direction));

			/*
			We then test whether the view ray intersects the sphere S or not. If it does,
			we compute an approximate (and biased) opacity value, using the same
			approximation as in GetSunVisibility:
			*/
			// Compute the distance between the view ray line and the sphere center,
			// and the distance between the camera and the intersection of the view
			// ray with the sphere (or NaN if there is no intersection).
			/*
			In the following we repeat the same steps as above, but for the planet sphere
			P instead of the sphere S (a smooth opacity is not really needed here, so we
			don't compute it. Note also how we modulate the sun and sky irradiance received
			on the ground by the sun and sky visibility factors):
			*/
			// Compute the distance between the view ray line and the Earth center,
			// and the distance between the camera and the intersection of the view
			// ray with the ground (or NaN if there is no intersection).
			float3 p = camera - earth_center;
			float p_dot_v = dot(p, view_direction);
			float p_dot_p = dot(p, p);
			float ray_earth_center_squared_distance = p_dot_p - p_dot_v * p_dot_v;
			float distance_to_intersection = -p_dot_v - sqrt(bottom_radius * bottom_radius - ray_earth_center_squared_distance);



			//huh, good info here... https://gamedev.stackexchange.com/questions/131978/shader-reconstructing-position-from-depth-in-vr-through-projection-matrix/140924#140924
			//https://forum.unity.com/threads/world-space-position-in-a-post-processing-shader.114392/
			//https://answers.unity.com/questions/1584649/how-do-you-port-old-shaders-to-hlsl-so-they-work-w.html
			//https://forum.unity.com/threads/screen-space-multiple-scattering.446647/page-3#post-3316350

			//fix for world-space recomposition from fulscreen triangle bullshit
			//https://grrava.blogspot.com/2018/08/more-foggy-adventures.html
			//https://michaldrobot.com/2014/04/01/gcn-execution-patterns-in-full-screen-passes/

			//0-1 linear depth value; 0= no depth, 1 = max depth
			//0 should be near clip plane, but it appears to actually be '0'
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv));// 1;//TODO - fix depth //float depth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r);

			//absolute world-space hit position (or should be)
			//camera position + near clip distance + percentage of distance between near and far clip in the depth buffer...
			//float3 worldPos = camera + i.view_ray2 + (depth * (i.view_ray - i.view_ray2));

			//distance from camera to the hit
			//float dist = length(i.view_ray2) + (depth * length(i.view_ray - i.view_ray2));//  length(camera - worldPos);
			float dist = depth * length(i.view_ray);// length(i.view_ray2) + (depth * length(i.view_ray - i.view_ray2));//  length(camera - worldPos);

			//distance from camera to planetary boundary
			//equals the distance to the planetary center minus distance to surface
			float dist2 = length(earth_center - camera) - (bottom_radius);
			float distance_to_intersection2 = -p_dot_v - sqrt(top_radius * top_radius - ray_earth_center_squared_distance);

			//but this is incorrect, as we actually need the distance for -this view ray- --if it intersects the surface--

			//if the distance from camera to hit is less than the distance between the camera and the atmosphere boundary
			//then it must have hit something -else- between the camera and the target planet
			//thus, discard the pixel / render zeros.

			if (dist * 1.001 < distance_to_intersection || dist < distance_to_intersection2)
			{
				return float4(0, 1, 0, 1);
				//return float4(dist.rrr / _ClipDepth, 1);
				//return float4(tex2D(_MainTex, i.uv).rgb, 1);
			}

			// Compute the radiance reflected by the ground, if the ray intersects it.
			float ground_alpha = 0.0;
			float3 ground_radiance = float3(0,0,0);
			if (distance_to_intersection > 0.0)
			{
				//TODO - this needs to utilize depth buffer to test for ground intersection
				//TODO - how to get a normal direction for the ground without a normals buffer?

				float3 _point = camera + view_direction * distance_to_intersection;
				float3 normal = normalize(_point - earth_center);

				// Compute the radiance reflected by the ground.
				float3 sky_irradiance;
				float3 sun_irradiance = GetSunAndSkyIrradiance(_point - earth_center, normal, sun_direction, sky_irradiance);

				float sunVis = 1;
				float skyVis = 1;

				ground_radiance = kGroundAlbedo * (1.0 / PI) * (sun_irradiance * sunVis + sky_irradiance * skyVis);

				float shadow_length = max(0.0, min(shadow_out, distance_to_intersection) - shadow_in) * lightshaft_fadein_hack;

				float3 transmittance;
				float3 in_scatter = GetSkyRadianceToPoint(camera - earth_center, _point - earth_center, shadow_length, sun_direction, transmittance);

				ground_radiance = ground_radiance * transmittance + in_scatter;
				ground_alpha = 1.0;
			}

			/*
			Finally, we compute the radiance and transmittance of the sky, and composite
			together, from back to front, the radiance and opacities of all the objects of
			the scene:
			*/

			// Compute the radiance of the sky.
			float shadow_length = 0;
			float3 transmittance;
			float3 radiance = GetSkyRadiance(camera - earth_center, view_direction, shadow_length, sun_direction, transmittance);

			// If the view ray intersects the Sun, add the Sun radiance.
			if (dot(view_direction, sun_direction) > sun_size.y)
			{
				radiance = radiance + transmittance * 1;// GetSolarRadiance();
			}

			radiance = lerp(radiance, ground_radiance, ground_alpha);
			radiance = pow(float3(1,1,1) - exp(-radiance / white_point * exposure), 1.0 / 2.2);

			float3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
			return float4(saturate(radiance.rgb + color.rgb), 1);
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
