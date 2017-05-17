// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef CUSTOM_GBUFFER_INCLUDED
#define CUSTOM_GBUFFER_INCLUDED

#include "r5g6b5.cginc"

//-----------------------------------------------------------------------------
// Main structure that store the data from the standard shader (i.e user input)
struct CustomData
{
    half3   diffuseColor;
    half    occlusion;

	half3   shadowColor;
    half3   specularColor;
    half    smoothness;

    half3   normalWorld;        // normal in world space

	half    edgeLight;
};

//-----------------------------------------------------------------------------
// This will encode UnityStandardData into GBuffer
void CustomDataToGbuffer(CustomData data, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    // RT0: diffuse color (rgb), occlusion (a) - sRGB rendertarget
	outGBuffer0 = half4(data.diffuseColor, data.smoothness);

    // RT1: spec color (rgb), smoothness (a) - sRGB rendertarget
	outGBuffer1 = half4(EncodeR5G6B5(data.specularColor), EncodeR5G6B5(data.shadowColor));

    // RT2: normal (rgb), --unused, very low precision-- (a)
	outGBuffer2 = half4(data.normalWorld * 0.5f + 0.5f, data.edgeLight);
}

//-----------------------------------------------------------------------------
// This decode the Gbuffer in a UnityStandardData struct
CustomData CustomDataFromGbuffer(half4 inGBuffer0, half4 inGBuffer1, half4 inGBuffer2)
{
    CustomData data;

	data.diffuseColor = inGBuffer0.rgb;
	data.smoothness = inGBuffer0.a;

	data.specularColor = DecodeR5G6B5(inGBuffer1.xy);
	data.shadowColor = DecodeR5G6B5(inGBuffer1.zw);

    data.normalWorld    = normalize(inGBuffer2.rgb * 2 - 1);
	data.edgeLight = inGBuffer2.a;

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

#endif // #ifndef CUSTOM_GBUFFER_INCLUDED
