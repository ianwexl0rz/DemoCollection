Shader "Hidden/TubeLight" {
SubShader {
	Tags { "Queue"="Geometry-1" }

CGINCLUDE
#include "UnityCG.cginc"
#include "CustomGBuffer.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityDeferredLibrary.cginc"

#define SHADOW_PLANES 1
#include "TubeLight.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

float _LightRadius;
float _LightLength;
float4 _LightAxis;


void DeferredCalculateLightParams (
	unity_v2f_deferred i,
	out float3 outWorldPos,
	out float2 outUV)
{
	i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	float2 uv = i.uv.xy / i.uv.w;
	
	// read depth and reconstruct world position
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	depth = Linear01Depth (depth);
	float4 vpos = float4(i.ray * depth,1);
	float3 wpos = mul (unity_CameraToWorld, vpos).xyz;

	outWorldPos = wpos;
	outUV = uv;
}

half4 CalculateLightDeferred (unity_v2f_deferred i, UNITY_VPOS_TYPE screenPos : SV_Position)
{
	float3 worldPos;
	float2 uv;
	DeferredCalculateLightParams (i, worldPos, uv);

	// unpack Gbuffer
	half4 gbuffer0 = tex2D(_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D(_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D(_CameraGBufferTexture2, uv);

	if (gbuffer2.a == 0)
	{
		half2 pixel = 1 / _ScreenParams;
		bool evenPixel = fmod(screenPos.y, 2) == fmod(screenPos.x, 2);

		float4 a1 = tex2D(_CameraGBufferTexture0, half2(uv.x + pixel.x, uv.y));
		float4 a2 = tex2D(_CameraGBufferTexture0, half2(uv.x - pixel.x, uv.y));
		float4 a3 = tex2D(_CameraGBufferTexture0, half2(uv.x, uv.y + pixel.y));
		float4 a4 = tex2D(_CameraGBufferTexture0, half2(uv.x, uv.y - pixel.y));

		float4 b1 = tex2D(_CameraGBufferTexture1, half2(uv.x + pixel.x, uv.y));
		float4 b2 = tex2D(_CameraGBufferTexture1, half2(uv.x - pixel.x, uv.y));
		float4 b3 = tex2D(_CameraGBufferTexture1, half2(uv.x, uv.y + pixel.y));
		float4 b4 = tex2D(_CameraGBufferTexture1, half2(uv.x, uv.y - pixel.y));

		half diffChroma = GetChroma(gbuffer0.rg, a1.rg, a2.rg, a3.rg, a4.rg);
		half specChroma = GetChromaWithType(gbuffer1.rgb, b1.rgb, b2.rgb, b3.rgb, b4.rgb);

		half3 baseColor = half3(gbuffer0.r, lerp(half2(diffChroma, gbuffer0.g), half2(gbuffer0.g, diffChroma), evenPixel));
		baseColor = YCoCgToRGB(baseColor);

		half3 specColor = half3(gbuffer1.r, lerp(half2(specChroma, gbuffer1.g), half2(gbuffer1.g, specChroma), evenPixel));
		specColor = gbuffer1.b > 0 ? gbuffer1.r : YCoCgToRGB(specColor);

		half oneMinusRoughness = gbuffer1.a;
		half3 normalWorld = gbuffer2.rgb * 2 - 1;
		normalWorld = normalize(normalWorld);

		return CalculateLight(worldPos, uv, baseColor, specColor, oneMinusRoughness, normalWorld,
			_LightPos.xyz, _LightPos.xyz + _LightAxis.xyz * _LightLength, _LightColor.xyz, _LightRadius, _LightPos.w);
	}
	else
	{
		half3 baseColor = gbuffer0.rgb;
		half3 specColor = gbuffer1.rgb;
		half oneMinusRoughness = gbuffer1.a;
		half3 normalWorld = gbuffer2.rgb * 2 - 1;
		normalWorld = normalize(normalWorld);
		
		return CalculateLight (worldPos, uv, baseColor, specColor, oneMinusRoughness, normalWorld,
			_LightPos.xyz, _LightPos.xyz + _LightAxis.xyz * _LightLength, _LightColor.xyz, _LightRadius, _LightPos.w);
	}
}
ENDCG

Pass {
	Fog { Mode Off }
	ZWrite Off
	Blend One One
	Cull Front
	ZTest Always

	
CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma exclude_renderers nomrt

fixed4 frag (unity_v2f_deferred i, UNITY_VPOS_TYPE screenPos : SV_Position) : SV_Target
{
	half4 light = CalculateLightDeferred(i, screenPos);
	// TODO: squash those NaNs at their source
	//return isnan(light) ? 0 : light;
	return light;
}

ENDCG
}

}
Fallback Off
}
