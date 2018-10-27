// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Custom-DeferredReflections" {
Properties {
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}
SubShader {

// Calculates reflection contribution from a single probe (rendered as cubes) or default reflection (rendered as full screen quad)
Pass {
	ZWrite Off
	ZTest LEqual
	Blend [_SrcBlend] [_DstBlend]
CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityPBSLighting.cginc"

#include "CustomGBuffer.cginc"
#include "CustomBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
	return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}

half4 frag (unity_v2f_deferred i, UNITY_VPOS_TYPE screenPos : SV_Position) : SV_Target
{
	// Stripped from UnityDeferredCalculateLightParams, refactor into function ?
	i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	float2 uv = i.uv.xy / i.uv.w;

	// read depth and reconstruct world position
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	depth = Linear01Depth (depth);
	float4 viewPos = float4(i.ray * depth,1);
	float3 worldPos = mul (unity_CameraToWorld, viewPos).xyz;

	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);

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
		//baseColor = YCoCgToRGB(baseColor);

		half3 specColor = half3(gbuffer1.r, lerp(half2(specChroma, gbuffer1.g), half2(gbuffer1.g, specChroma), evenPixel));
		//specColor = gbuffer1.b > 0 ? gbuffer1.r : YCoCgToRGB(specColor);

		CustomData data = CustomDataFromGbuffer(baseColor, specColor, gbuffer0, gbuffer1, gbuffer2);

		float3 eyeVec = normalize(worldPos - _WorldSpaceCameraPos);
		half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor);

		half3 worldNormalRefl = reflect(eyeVec, data.normalWorld);

		// Unused member don't need to be initialized
		UnityGIInput d;
		d.worldPos = worldPos;
		d.worldViewDir = -eyeVec;
		d.probeHDR[0] = unity_SpecCube0_HDR;

		float blendDistance = unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
		d.probePosition[0] = unity_SpecCube0_ProbePosition;
		d.boxMin[0].xyz = unity_SpecCube0_BoxMin - float4(blendDistance, blendDistance, blendDistance, 0);
		d.boxMin[0].w = 1;  // 1 in .w allow to disable blending in UnityGI_IndirectSpecular call
		d.boxMax[0].xyz = unity_SpecCube0_BoxMax + float4(blendDistance, blendDistance, blendDistance, 0);
#endif

		Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(data.smoothness, d.worldViewDir, data.normalWorld, data.specularColor);

		half3 env0 = UnityGI_IndirectSpecular(d, data.occlusion, g);

		UnityLight light;
		light.color = half3(0, 0, 0);
		light.dir = half3(0, 1, 0);

		UnityIndirect ind;
		ind.diffuse = 1;
		ind.specular = env0;

		half3 rgb = CUSTOM_BRDF(0, data.shadowColor, data.specularColor, 0, data.edgeLight, 1, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, 1, ind).rgb;

		//rgb = data.shadowColor * data.translucency * light.dir;

		// Calculate falloff value, so reflections on the edges of the probe would gradually blend to previous reflection.
		// Also this ensures that pixels not located in the reflection probe AABB won't
		// accidentally pick up reflections from this probe.
		half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
		half falloff = saturate(1.0 - length(distance) / blendDistance);

		return half4(rgb, falloff);
	}
	else
	{
		UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

		float3 eyeVec = normalize(worldPos - _WorldSpaceCameraPos);
		half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor);

		half3 worldNormalRefl = reflect(eyeVec, data.normalWorld);

		// Unused member don't need to be initialized
		UnityGIInput d;
		d.worldPos = worldPos;
		d.worldViewDir = -eyeVec;
		d.probeHDR[0] = unity_SpecCube0_HDR;
		d.boxMin[0].w = 1; // 1 in .w allow to disable blending in UnityGI_IndirectSpecular call since it doesn't work in Deferred

		float blendDistance = unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
		d.probePosition[0] = unity_SpecCube0_ProbePosition;
		d.boxMin[0].xyz = unity_SpecCube0_BoxMin - float4(blendDistance, blendDistance, blendDistance, 0);
		d.boxMax[0].xyz = unity_SpecCube0_BoxMax + float4(blendDistance, blendDistance, blendDistance, 0);
#endif

		Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(data.smoothness, d.worldViewDir, data.normalWorld, data.specularColor);

		half3 env0 = UnityGI_IndirectSpecular(d, data.occlusion, g);

		UnityLight light;
		light.color = half3(0, 0, 0);
		light.dir = half3(0, 1, 0);

		UnityIndirect ind;
		ind.diffuse = 0;
		ind.specular = env0;

		half3 rgb = UNITY_BRDF_PBS(0, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind).rgb;
		//half3 rgb = CUSTOM_BRDF(0, 0, data.specularColor, 0, 0, 1, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind).rgb;


		// Calculate falloff value, so reflections on the edges of the probe would gradually blend to previous reflection.
		// Also this ensures that pixels not located in the reflection probe AABB won't
		// accidentally pick up reflections from this probe.
		half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
		half falloff = saturate(1.0 - length(distance) / blendDistance);

		return half4(rgb, falloff);
	}
}

ENDCG
}

// Adds reflection buffer to the lighting buffer
Pass
{
	ZWrite Off
	ZTest Always
	Blend [_SrcBlend] [_DstBlend]

	CGPROGRAM
		#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile ___ UNITY_HDR_ON

		#include "UnityCG.cginc"

		sampler2D _CameraReflectionsTexture;

		struct v2f {
			float2 uv : TEXCOORD0;
			float4 pos : SV_POSITION;
		};

		v2f vert (float4 vertex : POSITION)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(vertex);
			o.uv = ComputeScreenPos (o.pos).xy;
			return o;
		}

		half4 frag (v2f i) : SV_Target
		{
			half4 c = tex2D (_CameraReflectionsTexture, i.uv);
			#ifdef UNITY_HDR_ON
			return float4(c.rgb, 0.0f);
			#else
			return float4(exp2(-c.rgb), 0.0f);
			#endif

		}
	ENDCG
}

}
Fallback Off
}
