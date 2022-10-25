#include "UnityDeferredLibrary.cginc"

half InvLerp(half value, half a, half b)
{
    return (value - a) / (b - a);
}

float3x3 RotationAlign(float3 d, float3 z)
{
    const float3  v = cross(z, d);
    const float c = dot(z, d);
    const float k = 1.0f / (1.0f + c);

    return float3x3(v.x*v.x*k + c,     v.y*v.x*k - v.z,    v.z*v.x*k + v.y,
                    v.x*v.y*k + v.z,   v.y*v.y*k + c,      v.z*v.y*k - v.x,
                    v.x*v.z*k - v.y,   v.y*v.z*k + v.x,    v.z*v.z*k + c   );
}

//smooth version of step
float aaStep(float compValue, float gradient){
    float halfChange = fwidth(gradient) / 2;
    //base the range of the inverse lerp on the change over one pixel
    float lowerEdge = compValue - halfChange;
    float upperEdge = compValue + halfChange;
    //do the inverse interpolation
    float stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
    stepped = saturate(stepped);
    return stepped;
}

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
    half3 halfDir = Unity_SafeNormalize (float3(light.dir) + viewDir);

    // IW: Extend the halfDir vector to flatten the specular falloff.
    halfDir *= 1 + perceptualRoughness * 0.02;

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

    half nlFull = dot(normal, light.dir);
    half nl = saturate(nlFull);
    half nh = saturate(dot(normal, halfDir));

    half lv = saturate(dot(light.dir, viewDir));
    half lh = saturate(dot(light.dir, halfDir));

    //half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
    //half diffuseTerm = smoothstep(0, 0.1, nl);
    //shadows = smoothstep(0, 0.1, shadows);

    // Specular term
    // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
    // BUT 1) that will make shader look significantly darker than Legacy ones
    // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

#if UNITY_BRDF_GGX
    half V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
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

    specularTerm *= smoothstep(0.5, 1, specularTerm) * 0.25;

    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));

    float diffuseTerm = smoothstep(0, 1/6.0, nlFull);
    //float diffuseTerm = aaStep(0, nlFull);// * smoothstep(-0.5, 0.5, nlFull);
    float hardShadows = smoothstep(1.0/3.0, 1.5/2.0, shadows);

    half3 color = diffColor * (gi.diffuse + light.color * diffuseTerm * hardShadows)
                //+ shadowColor * (1 - saturate(gi.diffuse + light.color * diffuseTerm * hardShadows))
                + specularTerm * light.color * FresnelTerm(specColor, lh) * hardShadows
                + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

#if defined (DIRECTIONAL)
    
    float distortion = 1;
    float power = 4;
    float scale = 4;
    float transmission = saturate(dot(-normal, light.dir) * 0.5 + 0.5) * shadows;;
    float3 ambient = transmission + lerp(shadows, 1, pow(translucency, 8));
    
    //Source: https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/
    float3 H = light.dir + normal * distortion;
    float VdotH = pow(saturate(dot(viewDir, -H)), power) * scale;
    float3 sss = light.color * (VdotH + ambient) * translucency;
    
    color += saturate(shadowColor) * sss;
#endif

    half3 edgeNormal = mul(RotationAlign(half3(0,0,1), viewDir), normal);
    edgeNormal.z *= smoothstep(0.3, 0.5, edgeNormal.z);
    edgeNormal = mul(RotationAlign(viewDir, half3(0,0,1)), edgeNormal);

    half edgeFresnel = pow(1 - saturate(dot(edgeNormal, viewDir)), 12);
    half edgeMask = smoothstep(-0.5, 1, dot(edgeNormal, light.dir)) * (1-lv);
    half edgeLight = aaStep(0.2, edgeFresnel) * edgeMask;
    color += edgeLight * edgeLightStrength * 4 * light.color * specColor;
    
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