#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

struct SSSData
{
    half3 color;
    half thickness;
    half ambient;
};

half RemapRange(half value, half oldMin, half oldMax, half newMin, half newMax)
{
    return newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin);
}

half InvLerp(half value, half a, half b)
{
    return (value - a) / (b - a);
}

half3 DirectBDRFStylized(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = SafeNormalize(lightDirectionWS + viewDirectionWS);

    half NoH = saturate(dot(normalWS, halfDir));
    half LoH = saturate(dot(lightDirectionWS, halfDir));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // BRDFspec = (D * V * F) / 4.0
    // D = roughness² / ( NoH² * (roughness² - 1) + 1 )²
    // V * F = 1.0 / ( LoH² * (roughness + 0.5) )
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155

    // Final BRDFspec = roughness² / ( NoH² * (roughness² - 1) + 1 )² * (LoH² * (roughness + 0.5) * 4.0)
    // We further optimize a few light invariant terms
    // brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
    half d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001h;

    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

    half NoL = saturate(dot(normalWS, lightDirectionWS));
    half3 edgeLight = (1.0 - saturate(dot(normalWS, viewDirectionWS))) * saturate(NoL);
    edgeLight = saturate(pow(edgeLight + 0.4, 32));

    /*
    if (brdfData.roughness2 < 1)
    {
        half hardEdge = saturate(InvLerp(specularTerm, saturate(0.5 - brdfData.roughness2), 1));
        specularTerm *= hardEdge;
    }
    */

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - HALF_MIN;
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

    //half3 color = max(specularTerm * brdfData.specular, edgeLight) + brdfData.diffuse;
    half3 color = max(specularTerm, edgeLight) * brdfData.specular + brdfData.diffuse;
    return color;
#else
    return brdfData.diffuse;
#endif
}

half3 LightingStylized(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS, half falloff)
{
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half celShading = saturate(NdotL / falloff);
    half3 radiance = lightColor * lightAttenuation * lerp(celShading, NdotL, 0);
    return DirectBDRFStylized(brdfData, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
}

half3 LightingStylized(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, half falloff)
{
    return LightingStylized(brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, falloff);
}

half4 LightweightFragmentStylized(InputData inputData, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha, half falloff)
{
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, occlusion, inputData.normalWS, inputData.viewDirectionWS);
    color += LightingStylized(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS, falloff);

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, inputData.positionWS);
        color += LightingStylized(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, falloff);
    }
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    color += inputData.vertexLighting * brdfData.diffuse;
#endif

    color += emission;
    return half4(color, alpha);
}

half3 LightingStylizedSSS(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS, half falloff, SSSData sssData)
{
    half NdotL = dot(normalWS, lightDirectionWS);
    half celShading = saturate(InvLerp(NdotL, 0, falloff)) * 0.8;
    half celShading2 = saturate(InvLerp(NdotL, 0.1, 0.6)) * 0.2;

    half3 sss = max(sssData.ambient, (1 - saturate(abs(NdotL / sssData.thickness)))) * sssData.color * sssData.thickness;
    half3 radiance = lightColor * lightAttenuation * (celShading + celShading2 + sss);

    return DirectBDRFStylized(brdfData, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
}

half3 LightingStylizedSSS(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, half falloff, SSSData sssData)
{
    return LightingStylizedSSS(brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, falloff, sssData);
}

half3 GlobalIlluminationStylized(BRDFData brdfData, half3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half fresnelTerm = Pow4(1.0 - saturate(dot(normalWS, viewDirectionWS)));

    half3 indirectDiffuse = bakedGI * occlusion;
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion);

    return EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
}

half4 LightweightFragmentStylizedSSS(InputData inputData, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha, half falloff, SSSData sssData)
{
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));
    
    half3 color = GlobalIlluminationStylized(brdfData, inputData.bakedGI, occlusion, inputData.normalWS, inputData.viewDirectionWS);
    color += LightingStylizedSSS(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS, falloff, sssData);

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, inputData.positionWS);
        color += LightingStylizedSSS(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, falloff, sssData);
    }
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    color += inputData.vertexLighting * brdfData.diffuse;
#endif

    color += emission;
    return half4(color, alpha);
}
