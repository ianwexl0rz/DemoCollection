// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_PBS_LIGHTING_INCLUDED
#define UNITY_PBS_LIGHTING_INCLUDED

#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityLightingCommon.cginc"
#include "UnityGBuffer.cginc"
#include "CustomGBuffer.cginc"
#include "CustomBRDF.cginc"
#include "UnityGlobalIllumination.cginc"

//-------------------------------------------------------------------------------------
// Default BRDF to use:
#if !defined (UNITY_BRDF_PBS) // allow to explicitly override BRDF in custom shader
    // still add safe net for low shader models, otherwise we might end up with shaders failing to compile
    #if SHADER_TARGET < 30 || defined(SHADER_TARGET_SURFACE_ANALYSIS) // only need "something" for surface shader analysis pass; pick the cheap one
        #define UNITY_BRDF_PBS BRDF3_Unity_PBS
    #elif defined(UNITY_PBS_USE_BRDF3)
        #define UNITY_BRDF_PBS BRDF3_Unity_PBS
    #elif defined(UNITY_PBS_USE_BRDF2)
        #define UNITY_BRDF_PBS BRDF2_Unity_PBS
    #elif defined(UNITY_PBS_USE_BRDF1)
        #define UNITY_BRDF_PBS BRDF1_Unity_PBS
    #else
        #error something broke in auto-choosing BRDF
    #endif
#endif

//-------------------------------------------------------------------------------------
// Custom Metallic workflow

struct SurfaceOutputCustom
{
    fixed3 Albedo;      // base (diffuse or specular) color
    fixed3 SecondaryColor;
    half Translucency;
    half EdgeLight;
    float3 Normal;      // tangent space normal, if written
    half3 Emission;
    half Metallic;      // 0=non-metal, 1=metal
    // Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
    // Everywhere in the code you meet smoothness it is perceptual smoothness
    half Smoothness;    // 0=rough, 1=smooth
    half Occlusion;     // occlusion (default 1)
    fixed Alpha;        // alpha for transparencies
    int2 ScreenPos;
};

inline half4 LightingCustom (SurfaceOutputCustom s, float3 viewDir, UnityGI gi)
{
    s.Normal = normalize(s.Normal);

    half oneMinusReflectivity;
    half3 specColor;
    s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
    half outputAlpha;
    s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

    bool evenPixel = fmod(s.ScreenPos.y, 2) == fmod(s.ScreenPos.x, 2);
    float3 diff = RGBToYCoCg(s.Albedo);
    float3 spec = RGBToYCoCg(specColor);
    spec.gb = s.Translucency == 0 ? spec.gb : RGBToYCoCg(s.SecondaryColor).gb;

    CustomData data;
    data.diffuseColor = half3(evenPixel ? diff.rg : diff.rb, 0);
    data.specularColor = half3(evenPixel ? spec.rg : spec.rb, 0);
    data.occlusion      = s.Occlusion;
    data.smoothness     = s.Smoothness;
    data.normalWorld    = half4(s.Normal, 0);
    data.shadowColor    = s.SecondaryColor;
    data.translucency   = s.Translucency;
    data.edgeLight      = s.EdgeLight;

    half4 c = CUSTOM_BRDF(data.diffuseColor, data.shadowColor, data.specularColor, data.translucency, data.edgeLight, 1, oneMinusReflectivity, data.smoothness, data.normalWorld, viewDir, gi.light, gi.indirect);
    c.a = outputAlpha;
    return c;
}

inline half4 LightingCustom_Deferred (SurfaceOutputCustom s, float3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    half oneMinusReflectivity;
    half3 specColor;
    s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    //half4 c = CUSTOM_BRDF (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
    half4 c = UNITY_BRDF_PBS (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);  

    bool evenPixel = fmod(s.ScreenPos.y, 2) == fmod(s.ScreenPos.x, 2);
    float3 diff = RGBToYCoCg(s.Albedo);
    float3 spec = RGBToYCoCg(specColor);
    spec.gb = s.Translucency == 0 ? spec.gb : RGBToYCoCg(s.SecondaryColor).gb;

    CustomData data;
    data.diffuseColor = half3(evenPixel ? diff.rg : diff.rb, 0);
    data.specularColor = half3(evenPixel ? spec.rg : spec.rb, 0);
    data.occlusion      = s.Occlusion;
    data.smoothness     = s.Smoothness;
    data.normalWorld    = half4(s.Normal, 0);
    data.shadowColor    = s.SecondaryColor;
    data.translucency   = s.Translucency;
    data.edgeLight      = s.EdgeLight;

    CustomDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    //half4 c = CUSTOM_BRDF(data.diffuseColor, data.shadowColor, data.specularColor, data.translucency, data.edgeLight, 1, oneMinusReflectivity, data.smoothness, data.normalWorld, viewDir, gi.light, gi.indirect);
    //half3 c = UNITY_BRDF_PBS (data.diffuseColor, data.specularColor , oneMinusReflectivity, data.smoothness, s.normalWorld, viewDir, gi.light, gi.indirect).rgb;

    half4 emission = half4(s.Emission + c.rgb, 1);
    return emission;
}

inline void LightingCustom_GI (
    SurfaceOutputCustom s,
    UnityGIInput data,
    inout UnityGI gi)
{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
#else
    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal, g);
#endif
}

#endif // UNITY_PBS_LIGHTING_INCLUDED
