// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef CUSTOM_GBUFFER_INCLUDED
#define CUSTOM_GBUFFER_INCLUDED

#define CUSTOM_USE_YCOCG

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
    half    smoothness;
    half3   normalWorld;        // normal in world space
    half    translucency;
	half    edgeLight;
};

//-----------------------------------------------------------------------------
// This will encode UnityStandardData into GBuffer
void CustomDataToGbuffer(CustomData data, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{

    // RT0: diffuse luma/chroma (rg), spec luma (b), spec/translucent chroma (a) - sRGB rendertarget
    outGBuffer0 = half4(data.diffuseColor.rg, data.specularColor.rg);

    // RT1: translucency (r), edgel light (g), occlusion (b), smoothness (a) - sRGB rendertarget
    outGBuffer1 = half4(data.translucency, data.edgeLight, data.occlusion, data.smoothness);

    // RT2: normal (rgb), --unused, very low precision-- (a)
	outGBuffer2 = half4(data.normalWorld * 0.5f + 0.5f, data.edgeLight);
}

//-----------------------------------------------------------------------------
// This decode the Gbuffer in a UnityStandardData struct
CustomData CustomDataFromGbuffer(half3 inGBuffer0RG, half3 inGBuffer0BA, half4 inGBuffer1, half4 inGBuffer2)
{

    CustomData data;

    data.diffuseColor = YCoCgToRGB(inGBuffer0RG);

    data.translucency = inGBuffer1.r;
    data.edgeLight = inGBuffer1.g;
    data.occlusion = inGBuffer1.b;
    data.smoothness = inGBuffer1.a;

    data.specularColor = data.translucency == 0 ? YCoCgToRGB(inGBuffer0BA) : inGBuffer0BA.xxx;
    data.shadowColor = data.translucency > 0 ? YCoCgToRGB(half3(data.translucency, inGBuffer0BA.gb)) : half3(0, 0, 0);

    data.normalWorld   = normalize(inGBuffer2.rgb * 2 - 1);

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
