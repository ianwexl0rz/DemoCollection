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
//#include "UnityStandardUtils.cginc"
#include "CustomGBuffer.cginc"
//#include "UnityGBuffer.cginc"
//#include "UnityStandardBRDF.cginc"

#include "CustomGBuffer.cginc"
#include "CustomBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

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

half4 CalculateLight (unity_v2f_deferred i, UNITY_VPOS_TYPE screenPos : SV_Position)
{
	float3 wpos;
	float2 uv;
	float atten, fadeDist;
	UnityLight light;
	UNITY_INITIALIZE_OUTPUT(UnityLight, light);
	UnityDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, fadeDist);

	light.color = _LightColor.rgb * atten;

	// unpack Gbuffer
	half4 gbuffer0 = tex2D(_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);

	// pixel offset
	half2 pixel = 1/_ScreenParams;

	#ifdef CUSTOM_USE_YCOCG
	float4 a1 = tex2D(_CameraGBufferTexture0, half2(uv.x + pixel.x, uv.y));
	float4 a2 = tex2D(_CameraGBufferTexture0, half2(uv.x - pixel.x, uv.y));
	float4 a3 = tex2D(_CameraGBufferTexture0, half2(uv.x, uv.y + pixel.y));
	float4 a4 = tex2D(_CameraGBufferTexture0, half2(uv.x, uv.y - pixel.y));

	float b1 = tex2D(_CameraGBufferTexture1, half2(uv.x + pixel.x, uv.y)).r;
	float b2 = tex2D(_CameraGBufferTexture1, half2(uv.x - pixel.x, uv.y)).r;
	float b3 = tex2D(_CameraGBufferTexture1, half2(uv.x, uv.y + pixel.y)).r;
	float b4 = tex2D(_CameraGBufferTexture1, half2(uv.x, uv.y - pixel.y)).r;

	bool evenPixel = fmod(screenPos.y, 2) == fmod(screenPos.x, 2);

	half3 unpackDiffuse = gbuffer0.rgg;
	half3 unpackSpec = gbuffer0.baa;

	half2 unfilteredChroma = filter(gbuffer0, a1, a2, a3, a4, gbuffer1.r, b1, b2, b3, b4);

	unpackDiffuse.b = !evenPixel ? unpackDiffuse.g : unfilteredChroma.x;
	unpackDiffuse.g = evenPixel ? unpackDiffuse.g : unfilteredChroma.x;

	unpackSpec.b = !evenPixel ? unpackSpec.g : unfilteredChroma.y;
	unpackSpec.g = evenPixel ? unpackSpec.g : unfilteredChroma.y;
	#endif

	CustomData data = CustomDataFromGbuffer(unpackDiffuse, unpackSpec, gbuffer1, gbuffer2);

	//if(data.normalWorld.z < 0.0f) data.normalWorld.z = 0.0f;

	float3 eyeVec = normalize(wpos-_WorldSpaceCameraPos);
	half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor.rgb);

	UnityIndirect ind;
	UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
	ind.diffuse = 0;
	ind.specular = 0;

    half4 res = CustomLighting (data.diffuseColor, data.shadowColor, data.specularColor, data.translucency, data.edgeLight, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind);

	return res;
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
