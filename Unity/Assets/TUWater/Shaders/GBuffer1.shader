Shader "TU/GBuffer1"
{

	HLSLINCLUDE	
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		//frustum bounding vectors for far clip, used to determine world-space view direction from inside screen space shader
		float3 _Left;
		float3 _Right;
		float3 _Left2;
		float3 _Right2;	

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(_BumpMap, sampler_BumpMap);

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

			return float4(backgroundColor.rgb,1);
		}

	ENDHLSL

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite On ZTest LEqual

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			ENDHLSL
		}

	}

}
