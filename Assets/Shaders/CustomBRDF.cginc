#include "UnityDeferredLibrary.cginc"

// Main Physically Based BRDF
// Derived from Disney work and based on Torrance-Sparrow micro-facet model
//
//   BRDF = kD / pi + kS * (D * V * F) / 4
//   I = BRDF * NdotL
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) Normalized BlinnPhong
//  b) GGX
// * Smith for Visiblity term
// * Schlick approximation for Fresnel
half4 CUSTOM_BRDF (half3 diffColor, half3 shadowColor, half3 specColor, half3 translucency, half edgeLightStrength, half hardness, half oneMinusReflectivity, half smoothness,
    half3 normal, half3 viewDir,
    UnityLight light, half shadows, UnityIndirect gi)
{

    half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
    half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);

    // Stylize the specular
    half3 sharpHalfDir = smoothstep(0, smoothness, halfDir * smoothness);
    halfDir = lerp(halfDir, sharpHalfDir, 1 - step(smoothness, 0));

// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0

#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
    // The amount we shift the normal toward the view vector is defined by the dot product.
    half shiftAmount = dot(normal, viewDir);
    normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
    // A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
    //normal = normalize(normal);

    half nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
#else
    half nv = abs(dot(normal, viewDir));    // This abs allow to limit artifact
#endif

	half nlUnclamped = dot(normal, light.dir);
    half nl = saturate(nlUnclamped);
    half nh = saturate(dot(normal, halfDir));

    half lv = saturate(dot(light.dir, viewDir));
    half lh = saturate(dot(light.dir, halfDir));

    half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
    half diffuseTermModified = smoothstep(0, 0.1, diffuseTerm);
    diffuseTerm = lerp(diffuseTerm, diffuseTermModified, 0.6 * hardness);

    float fresnel = smoothstep(0.5, 0.4, nv);
    float edgelight = saturate((nl - nv) * 4) * fresnel;

    // Specular term
    // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
    // BUT 1) that will make shader look significantly darker than Legacy ones
    // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

#if UNITY_BRDF_GGX
    half V = SmithJointGGXVisibilityTerm (nl * 3, nv * 3, roughness);
    half D = GGXTerm (nh, roughness);
#else
    // Legacy
    half V = SmithBeckmannVisibilityTerm(nl, nv, roughness);
    half D = NDFBlinnPhongNormalizedTerm (nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
#endif

    half specularTerm = V * D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

#   ifdef UNITY_COLORSPACE_GAMMA
        specularTerm = sqrt(max(1e-4h, specularTerm));
#   endif

    // specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
    specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
    half surfaceReduction;
#   ifdef UNITY_COLORSPACE_GAMMA
        surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#   else
        surfaceReduction = 1.0 / (roughness*roughness + 1.0);           // fade \in [0.5;1]
#   endif

    // To provide true Lambert lighting, we need to be able to kill specular completely.
    specularTerm *= any(specColor) ? 1.0 : 0.0;

    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));

    float distortion = 0.5;
    float power = 1;
    float scale = 2;

    //Source: https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/
    float3 H = light.dir + normal * distortion;
    float VdotH = pow(saturate(dot(viewDir, -H)), power) * scale;
    float3 sss = light.color * (VdotH /*+ Ambient*/) * translucency;

    half transPow = pow(translucency,8);
    half3 spec = specularTerm * FresnelTerm(specColor, lh);

    float halfLambert = nlUnclamped * 0.5 + 0.5;
    float halfLdotV = dot(light.dir + normal * distortion, viewDir) * 0.5 + 0.5;

    // Darken diffuse in the middle if translucent
    diffColor *= lerp(1, max(1 - pow(nv, 2), diffColor), translucency * (1 - lv));

    half3 color = 
        diffColor * (gi.diffuse + light.color * max(shadows * diffuseTerm, lerp(0, shadowColor, transPow)))
        + max(edgelight * edgeLightStrength * specColor * (1 + diffColor) * 4, spec) * light.color * shadows
        + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

    half fadedShadows = lerp(shadows, 1, transPow * 0.25);
    half transmission = halfLdotV * (1-halfLambert) * translucency;
    transmission = pow(transmission * scale, 2);
    
    color += max(shadowColor, spec) * max(sss, transmission) * fadedShadows;

    return half4(color, 1);
}

// Common lighting data calculation (direction, attenuation, ...)
void CustomDeferredCalculateLightParams (
    unity_v2f_deferred i,
    out float3 outWorldPos,
    out float2 outUV,
    out half3 outLightDir,
    out float outAtten,
    out float outFadeDist,
    out float outShadows)
{
    i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
    float2 uv = i.uv.xy / i.uv.w;

    // read depth and reconstruct world position
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
    depth = Linear01Depth (depth);
    float4 vpos = float4(i.ray * depth,1);
    float3 wpos = mul (unity_CameraToWorld, vpos).xyz;

    float fadeDist = UnityComputeShadowFadeDistance(wpos, vpos.z);

    // spot light case
    #if defined (SPOT)
        float3 tolight = _LightPos.xyz - wpos;
        half3 lightDir = normalize (tolight);

        float4 uvCookie = mul (unity_WorldToLight, float4(wpos,1));
        // negative bias because http://aras-p.info/blog/2010/01/07/screenspace-vs-mip-mapping/
        float atten = tex2Dbias (_LightTexture0, float4(uvCookie.xy / uvCookie.w, 0, -8)).w;
        atten *= uvCookie.w < 0;
        float att = dot(tolight, tolight) * _LightPos.w;
        atten *= tex2D (_LightTextureB0, att.rr).r;

        float shadows = UnityDeferredComputeShadow (wpos, fadeDist, uv);

    // directional light case
    #elif defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
        half3 lightDir = -_LightDir.xyz;
        float atten = 1.0;

        float shadows = UnityDeferredComputeShadow (wpos, fadeDist, uv);

        #if defined (DIRECTIONAL_COOKIE)
        atten *= tex2Dbias (_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xy, 0, -8)).w;
        #endif //DIRECTIONAL_COOKIE

    // point light case
    #elif defined (POINT) || defined (POINT_COOKIE)
        float3 tolight = wpos - _LightPos.xyz;
        half3 lightDir = -normalize (tolight);

        float att = dot(tolight, tolight) * _LightPos.w;
        float atten = tex2D (_LightTextureB0, att.rr).r;

        float shadows = UnityDeferredComputeShadow (tolight, fadeDist, uv);

        #if defined (POINT_COOKIE)
        atten *= texCUBEbias(_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xyz, -8)).w;
        #endif //POINT_COOKIE
    #else
        half3 lightDir = 0;
        float atten = 0;
        float shadows = 0;
    #endif

    outWorldPos = wpos;
    outUV = uv;
    outLightDir = lightDir;
    outAtten = atten;
    outFadeDist = fadeDist;
    outShadows = shadows;
}