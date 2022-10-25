// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef CUSTOM_GBUFFER_INCLUDED
#define CUSTOM_GBUFFER_INCLUDED

//#define USE_INTERLACED_YCOCG
#define USE_CUSTOM_FOR_STANDARD

#include "RGB555.cginc"
#include "YCoCG.cginc"
#include "ColorSpace.cginc"

//-----------------------------------------------------------------------------
// Main structure that store the data from the standard shader (i.e user input)
struct CustomData
{
    half3   diffuseColor;
    half    occlusion;
	half3   shadowColor;
    half3   specularColor;
	half3   packedData;
    half    smoothness;
    half4   normalWorld;        // normal in world space
    half    translucency;
	half    edgeLight;
	half    depth;
};

// This will encode CustomData into GBuffer
void PackedDataToGbuffer(CustomData data, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
	// RT0: diffuse (rgb), occlusion (a) - sRGB rendertarget
	outGBuffer0 = half4(data.diffuseColor, data.occlusion);

	// RT1: specular (rgb), smoothness (a) - sRGB rendertarget
	outGBuffer1 = half4(data.packedData, data.smoothness);

	// RT2: normal (rgb), --unused, very low precision-- (a)
	outGBuffer2 = half4(data.normalWorld.rgb * 0.5f + 0.5f, data.normalWorld.a);
}

CustomData DataFromPackedGbuffer(half4 inGBuffer0, half4 inGBuffer1, half4 inGBuffer2)
{
	CustomData data;

	half materialId = inGBuffer2.a * 3;
	half metallic = materialId == 1 ? inGBuffer1.r : 0;
	
	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic (inGBuffer0.rgb, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);
	half3 unpackedData = DecodeR5G5B5(inGBuffer1.gb);
	
	data.diffuseColor = diffColor;
	data.specularColor = specColor;
	data.occlusion = inGBuffer0.a;
	data.smoothness = inGBuffer1.a;
	data.translucency = materialId == 2 ? inGBuffer1.r : 0;
	data.shadowColor = materialId < 3 ? YCoCgToRGB(half3(0, unpackedData.rg)) : half3(0, 0, 0);
	data.edgeLight = materialId < 3 ? unpackedData.b : 0;
	data.normalWorld.rgb = normalize(inGBuffer2.rgb * 2 - 1);

	return data;
}

//-----------------------------------------------------------------------------
// In some cases like for terrain, the user want to apply a specific weight to the attribute
// The function below is use for this
void CustomDataApplyWeightToGbuffer(inout half4 inOutGBuffer0, inout half4 inOutGBuffer1, inout half4 inOutGBuffer2, half alpha)
{
    // With UnityStandardData current encoding, We can apply the weigth directly on the gbuffer
    inOutGBuffer0.rgb   *= alpha; // diffuseColor
    inOutGBuffer1       *= alpha; // SpecularColor and Smoothness
    inOutGBuffer2.rgb   *= alpha; // Normal
}
//-----------------------------------------------------------------------------

//Source: http://graphics.cs.aueb.gr/graphics/docs/papers/YcoCgFrameBuffer.pdf
//Returns the missing chrominance (Co or Cg) of a pixel.
//a1-a4 are the 4 neighbors of the center pixel a0.
float GetChroma(float2 a0, float2 a1, float2 a2, float2 a3, float2 a4)
{
	float4 lum = float4(a1.x, a2.x, a3.x, a4.x);
	float4 w = 1.0 - step(128.0 / 255.0, abs(lum - a0.x));

	float W = w.x + w.y + w.z + w.w;

	// handle the special case where all the weights are zero
	w.x = (W == 0.0) ? 1.0 : w.x; W = (W == 0.0) ? 1.0 : W;
	return (w.x * a1.y + w.y * a2.y + w.z * a3.y + w.w * a4.y) / W;
}

// lum, chroma, translucency
float GetChromaWithType(float3 a0, float3 a1, float3 a2, float3 a3, float3 a4)
{
	float4 lum = float4(a1.x, a2.x, a3.x, a4.x);
	float4 w = 1.0 - step(30.0 / 255.0, abs(lum - a0.x));

	// only blend if same type
	w *= float4(a1.z, a2.z, a3.z, a4.z);

	float W = w.x + w.y + w.z + w.w;

	// handle the special case where all the weights are zero
	w.x = (W == 0.0) ? 1.0 : w.x; W = (W == 0.0) ? 1.0 : W;
	return (w.x * a1.y + w.y * a2.y + w.z * a3.y + w.w * a4.y) / W;
}

#endif // #ifndef CUSTOM_GBUFFER_INCLUDED
