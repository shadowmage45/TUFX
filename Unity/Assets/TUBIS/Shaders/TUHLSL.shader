Shader "TU/HLSL"
{

	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	HLSLINCLUDE
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
		//sampler2D _MainTex;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;
			v.vertex.z = 0.0;
			o.vertex = mul(mul(unity_MatrixVP, unity_ObjectToWorld), (v.vertex));
			o.uv = v.uv;
			return o;
		}

		float4 frag(v2f i) : SV_TARGET
		{
			return float4(1,0,0,1);
			//return float4(i.uv.x,i.uv.y,0,1);
			//float3 col = tex2D(_MainTex, i.uv).rgb;
			//return float4(saturate(radiance + col), 1);
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
			
			//#include "UnityCG.cginc"
			#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

			ENDHLSL
		}
	}
}
