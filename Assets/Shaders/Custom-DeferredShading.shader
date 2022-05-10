// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Custom-DeferredShading" {
Properties {
	_LightTexture0 ("", any) = "" {}
	_LightTextureB0 ("", 2D) = "" {}
	_ShadowMapTexture ("", any) = "" {}
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}
SubShader {

// Pass 1: Lighting pass
//  LDR case - Lighting encoded into a subtractive ARGB8 buffer
//  HDR case - Lighting additively blended into floating point buffer
Pass {
	ZWrite Off
	Blend [_SrcBlend] [_DstBlend]

CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma exclude_renderers nomrt

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
//#include "CustomPBSLighting.cginc"
//#include "UnityStandardUtils.cginc"
//#include "UnityGBuffer.cginc"
//#include "UnityStandardBRDF.cginc"

#include "CustomGBuffer.cginc"
#include "CustomBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

half2 SobelDepth(sampler2D tex, half centerPixel, half2 uv)
{
	half2 pixel = 1 / _ScreenParams;
	half rightPixel = tex2D(tex, half2(uv.x + pixel.x, uv.y)).r;
	half leftPixel = tex2D(tex, half2(uv.x - pixel.x, uv.y)).r;
	half upPixel = tex2D(tex, half2(uv.x, uv.y + pixel.y)).r;
	half downPixel = tex2D(tex, half2(uv.x, uv.y - pixel.y)).r;

	// return abs(leftPixel - centerPixel) +
	// 	   abs(rightPixel - centerPixel) +
	// 	   abs(upPixel - centerPixel) +
	// 	   abs(downPixel - centerPixel);

	half leftDelta = leftPixel - centerPixel;
	half rightDelta = centerPixel - rightPixel;
	half upDelta = centerPixel - upPixel;
	half downDelta = downPixel - centerPixel;
	
	half lowEdge = 0.0;
	half highEdge = 0.001;
	
	half2 packedNormal = half2(smoothstep(lowEdge, highEdge, rightDelta) - smoothstep(lowEdge, highEdge, leftDelta),
		   smoothstep(lowEdge, highEdge, upDelta) - smoothstep(lowEdge, highEdge, downDelta));

	//half3 normal = half3(packedNormal.xy, 0);

	//normal.z = sqrt(1 - packedNormal.x * packedNormal.x - packedNormal.y * packedNormal.y);

	return packedNormal;
	
}

half4 CalculateLight (unity_v2f_deferred i, UNITY_VPOS_TYPE screenPos : SV_Position)
{
	float3 wpos;
	float2 uv;
	float atten, fadeDist, shadows;
	UnityLight light;
	UNITY_INITIALIZE_OUTPUT(UnityLight, light);
	CustomDeferredCalculateLightParams(i, wpos, uv, light.dir, atten, fadeDist, shadows);

	light.color = _LightColor.rgb * atten;

	// unpack Gbuffer
	float4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	float4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	float4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);

	//half2 sobel = SobelDepth(_CameraGBufferTexture1, gbuffer1.r, uv);
	//gbuffer0.rg += sobel;
	
	if (gbuffer2.a < 1)
	{
		CustomData data = DataFromPackedGbuffer(gbuffer0, gbuffer1, gbuffer2);
		
		float3 eyeVec = normalize(wpos - _WorldSpaceCameraPos);
		half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor);

		UnityIndirect ind;
		UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
		ind.diffuse = 0;
		ind.specular = 0;

		half4 res = CUSTOM_BRDF(data.diffuseColor, data.shadowColor, data.specularColor, data.translucency, data.edgeLight, 1, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, shadows, ind);
		return res;
	}
	else
	{
		light.color *= shadows;

#ifdef USE_CUSTOM_FOR_STANDARD
		CustomData data = DataFromPackedGbuffer(gbuffer0, gbuffer1, gbuffer2);
		half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor);
#else

		half3 specColor;
		half oneMinusReflectivity;
		half3 diffColor = DiffuseAndSpecularFromMetallic (gbuffer0.rgb, gbuffer1.r, /*out*/ specColor, /*out*/ oneMinusReflectivity);

		gbuffer0.rgb = diffColor;
		gbuffer1.rgb = specColor;
		
		UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
#endif
		
		float3 eyeVec = normalize(wpos - _WorldSpaceCameraPos);

		UnityIndirect ind;
		UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
		ind.diffuse = 0;
		ind.specular = 0;

#ifdef USE_CUSTOM_FOR_STANDARD
		half4 res = CUSTOM_BRDF(data.diffuseColor, data.shadowColor, data.specularColor, data.translucency, data.edgeLight, 1, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, shadows, ind);
#else
		half4 res = UNITY_BRDF_PBS(data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind);
#endif

		return res;
	}
}

#ifdef UNITY_HDR_ON
half4
#else
fixed4
#endif
frag (unity_v2f_deferred i, UNITY_VPOS_TYPE screenPos : SV_Position) : SV_Target
{
	half4 c = CalculateLight(i, screenPos);
	#ifdef UNITY_HDR_ON
	return c;
	#else
	return exp2(-c);
	#endif
}

ENDCG
}


// Pass 2: Final decode pass.
// Used only with HDR off, to decode the logarithmic buffer into the main RT
Pass {
	ZTest Always Cull Off ZWrite Off
	Stencil {
		ref [_StencilNonBackground]
		readmask [_StencilNonBackground]
		// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
		compback equal
		compfront equal
	}

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers nomrt

#include "UnityCG.cginc"

sampler2D _LightBuffer;
struct v2f {
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(vertex);
	o.texcoord = texcoord.xy;
#ifdef UNITY_SINGLE_PASS_STEREO
	o.texcoord = TransformStereoScreenSpaceTex(o.texcoord, 1.0f);
#endif
	return o;
}

fixed4 frag (v2f i) : SV_Target
{
	return -log2(tex2D(_LightBuffer, i.texcoord));
}
ENDCG 
}

}
Fallback Off
}
