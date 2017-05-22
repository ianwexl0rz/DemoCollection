// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef CUSTOM_GBUFFER_INCLUDED
#define CUSTOM_GBUFFER_INCLUDED

//#define CUSTOM_USE_YCOCG

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
	half    edgeLight;
};

//-----------------------------------------------------------------------------
// This will encode UnityStandardData into GBuffer
void CustomDataToGbuffer(CustomData data, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{

    #ifdef CUSTOM_USE_YCOCG
        outGBuffer0 = half4(RGBToYCoCg(data.diffuseColor), data.smoothness);
    #else
        // RT0: diffuse color (rgb), occlusion (a) - sRGB rendertarget
        outGBuffer0 = half4(data.diffuseColor, data.smoothness);
    #endif

    // RT1: spec color (rgb), smoothness (a) - sRGB rendertarget
    outGBuffer1 = half4(EncodeR5G5B5(data.specularColor), EncodeR5G5B5(data.shadowColor));
    // RT2: normal (rgb), --unused, very low precision-- (a)
	outGBuffer2 = half4(data.normalWorld * 0.5f + 0.5f, data.edgeLight);

    #ifndef UNITY_COLORSPACE_GAMMA
    #ifdef CUSTOM_USE_YCOCG
        outGBuffer0.rgb = GammaToLinear(outGBuffer0.rgb);
    #endif
        outGBuffer1.rgb = GammaToLinear(outGBuffer1.rgb);
    #endif
}

//-----------------------------------------------------------------------------
// This decode the Gbuffer in a UnityStandardData struct
CustomData CustomDataFromGbuffer(half4 inGBuffer0, half4 inGBuffer1, half4 inGBuffer2)
{

    #ifndef UNITY_COLORSPACE_GAMMA
    #ifdef CUSTOM_USE_YCOCG
        inGBuffer0.rgb = LinearToGamma(inGBuffer0.rgb);
    #endif
        inGBuffer1.rgb = LinearToGamma(inGBuffer1.rgb);
    #endif

    CustomData data;

    #ifdef CUSTOM_USE_YCOCG
        data.diffuseColor = YCoCgToRGB(inGBuffer0.rgb);
    #else
        data.diffuseColor = inGBuffer0.rgb;
    #endif

    data.smoothness    = inGBuffer0.a;
    data.specularColor = DecodeR5G5B5(inGBuffer1.xy);
    data.shadowColor   = DecodeR5G5B5(inGBuffer1.zw);
    data.normalWorld   = normalize(inGBuffer2.rgb * 2 - 1);
    data.edgeLight     = inGBuffer2.a;
    data.occlusion     = 1;

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
