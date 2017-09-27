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

//Returns the missing chrominance (Co or Cg) of a pixel.
//a1-a4 are the 4 neighbors of the center pixel a0.
float2 filter(float3 a0, float4 a1, float4 a2, float4 a3, float4 a4, float b0, float b1, float b2, float b3, float b4)
{
	float4 lum = float4(a1.x, a2.x , a3.x, a4.x);
	float4 w = 1.0 - step(30.0/255.0, abs(lum - a0.x));

	// don't blend if luma is zero
	w *= float4(a1.x, a2.x , a3.x, a4.x) > 0;

	float W = w.x + w.y + w.z + w.w;
	// handle the special case where all the weights are zero
	w.x = (W==0.0)? 1.0: w.x; W = (W==0.0)? 1.0: W;

	float diffChroma = half(w.x * a1.y + w.y * a2.y + w.z * a3.y + w.w * a4.y) / W;

	// Next get the missing chroma for spec / translucent
	lum = float4(a1.z, a2.z , a3.z, a4.z);
	w = 1.0 - step(30.0/255.0, abs(lum - a0.z));

	// don't blend if luma is zero
	w *= float4(a1.z, a2.z , a3.z, a4.z) > 0;

	// only blend if chroma is same type
	w *= b0 > 0 == float4(b1, b2, b3, b4) > 0;

	W = w.x + w.y + w.z + w.w;
	// handle the special case where all the weights are zero
	w.x = (W==0.0)? 1.0: w.x; W = (W==0.0)? 1.0: W;

	float specChroma = half(w.x * a1.w + w.y * a2.w+w.z * a3.w+w.w * a4.w) / W;
	return float2(diffChroma, specChroma);
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

		#ifdef CUSTOM_USE_YCOCG
	float4 a1 = tex2D(_CameraGBufferTexture0, half2(uv.x + 1/_ScreenParams.x, uv.y));
	float4 a2 = tex2D(_CameraGBufferTexture0, half2(uv.x - 1/_ScreenParams.x, uv.y));
	float4 a3 = tex2D(_CameraGBufferTexture0, half2(uv.x, uv.y + 1/_ScreenParams.y));
	float4 a4 = tex2D(_CameraGBufferTexture0, half2(uv.x, uv.y - 1/_ScreenParams.y));

	float b1 = tex2D(_CameraGBufferTexture1, half2(uv.x + 1/_ScreenParams.x, uv.y)).g;
	float b2 = tex2D(_CameraGBufferTexture1, half2(uv.x - 1/_ScreenParams.x, uv.y)).g;
	float b3 = tex2D(_CameraGBufferTexture1, half2(uv.x, uv.y + 1/_ScreenParams.y)).g;
	float b4 = tex2D(_CameraGBufferTexture1, half2(uv.x, uv.y - 1/_ScreenParams.y)).g;

	bool evenPixel = fmod(screenPos.y, 2) == fmod(screenPos.x, 2);

	half3 unpackDiffuse = gbuffer0.rgg;
	half3 unpackSpec = gbuffer0.baa;

	half2 unfilteredChroma = filter(gbuffer0, a1, a2, a3, a4, gbuffer1.r, b1, b2, b3, b4);

	unpackDiffuse.b = !evenPixel ? unpackDiffuse.g : unfilteredChroma.x;
	unpackDiffuse.g = evenPixel ? unpackDiffuse.g : unfilteredChroma.x;

	unpackSpec.b = !evenPixel ? unpackSpec.g : unfilteredChroma.y;
	unpackSpec.g = evenPixel ? unpackSpec.g : unfilteredChroma.y;
	#endif

	// gbuffer0.b is the luma component
	CustomData data = CustomDataFromGbuffer(unpackDiffuse, unpackSpec, gbuffer1, gbuffer2);

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
	d.probePosition[0]	= unity_SpecCube0_ProbePosition;
	d.boxMin[0].xyz		= unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
	d.boxMin[0].w		= 1;  // 1 in .w allow to disable blending in UnityGI_IndirectSpecular call
	d.boxMax[0].xyz		= unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
	#endif

	Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(data.smoothness, d.worldViewDir, data.normalWorld, data.specularColor);

	half3 env0 = UnityGI_IndirectSpecular(d, data.occlusion, g);

	UnityLight light;
	light.color =  half3(0, 0, 0);
	light.dir = half3(0, 1, 0);

	UnityIndirect ind;
	ind.diffuse = 1;
	ind.specular = env0;

	half3 rgb = CustomLighting (0, data.shadowColor, data.specularColor, data.translucency, data.edgeLight, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind).rgb;

	//rgb = data.shadowColor * data.translucency * light.dir;

	// Calculate falloff value, so reflections on the edges of the probe would gradually blend to previous reflection.
	// Also this ensures that pixels not located in the reflection probe AABB won't
	// accidentally pick up reflections from this probe.
	half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
	half falloff = saturate(1.0 - length(distance)/blendDistance);

	return half4(rgb, falloff);
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
