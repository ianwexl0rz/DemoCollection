﻿Shader "Hidden/Custom/Grayscale"
{
	HLSLINCLUDE

#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	float _Blend;

	float4 Frag(VaryingsDefault i) : SV_Target
	{
		float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
		float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
		color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);
		return color;
	}

		ENDHLSL

		SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Stencil{
			Ref 2
			ReadMask 2
			Comp Equal
			Pass Keep
			Fail Keep
		}

		Pass
		{
			HLSLPROGRAM

				#pragma vertex VertDefault
				#pragma fragment Frag

			ENDHLSL
		}
	}
}