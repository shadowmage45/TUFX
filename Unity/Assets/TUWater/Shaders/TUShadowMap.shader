Shader "TU/ShadowMap"
{
	HLSLINCLUDE
		//this is basically some slapped together/hacked monstrosity of CG and HLSL combined...
#include "UnityCG.cginc"
//as it is basically a geometry shader, don't neeed all the screen-space stuff from post-process package
//#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		float _LinearDepth;

		struct at
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
		};

		struct vertout
		{
			float4 vertex : SV_POSITION;			
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
			float4 vertexw : TEXCOORD1;
		};

		vertout vert(at v)
		{
			vertout o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.vertexw = mul(unity_ObjectToWorld, v.vertex);
			o.uv = v.uv;
			o.normal = UnityObjectToWorldNormal(v.normal);
			return o;
		}

		float4 frag(vertout i) : SV_TARGET
		{
			float3 cam = _WorldSpaceCameraPos;
			float depth = length(i.vertexw - cam);
			depth = depth / (_LinearDepth);			
			//depth = 1 - depth;
			return float4(depth.rrr, 1);
		}

	ENDHLSL

	SubShader
	{
		//standard opaque shader properties
		Cull Back ZWrite On ZTest LEqual

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			ENDHLSL
		}

	}

}
