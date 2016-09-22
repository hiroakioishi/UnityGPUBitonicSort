Shader "Hidden/BitonicSort"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
		
	
	sampler2D _MainTex;
	float4 _MainTex_ST;

	float4 _TextureSize;
	float  _SortBlockSize;
	float  _SortSize;
	float  _Offset;

	float2 convert1dto2d(float index)
	{
		float2 dst;

		dst.x = fmod(index, _TextureSize.x) / _TextureSize.x;
		dst.y = floor(index / _TextureSize.x) / _TextureSize.y;

		return dst;
	}

	
	float4 frag(v2f_img i) : SV_Target
	{
		float4 dst = (float4)0;

		float2 index2d = _TextureSize.xy * i.uv.xy;
		index2d = floor(index2d);

		float index1d = index2d.y * _TextureSize.x + index2d.x;

		float csign = (fmod(index1d, _SortBlockSize) < _Offset) ? 1 : -1;
		float cdir  = (fmod(floor(index1d / _SortSize), 2.0) <= 0.5) ? 1 : -1;

		float4 val0 = tex2D(_MainTex, i.uv.xy);

		float  adr1d = csign * _Offset + index1d;
		float2 adr2d = convert1dto2d(adr1d);
		float4 val1  = tex2D(_MainTex, adr2d);

		// a成分をソートのキーとして使用
		float4 cmin = (val0.a < val1.a) ? val0 : val1;
		float4 cmax = (val0.a < val1.a) ? val1 : val0;
		dst = (csign == cdir) ? cmin : cmax;

		return dst;
	}
	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
			LOD 100

			Pass
		{
			CGPROGRAM
			#pragma vertex   vert_img
			#pragma fragment frag
			ENDCG
		}
	}
}