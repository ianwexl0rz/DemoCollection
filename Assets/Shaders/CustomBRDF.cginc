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
    UnityLight light, UnityIndirect gi)
{

    half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
    half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);

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

    float fresnel = smoothstep(0.5, 0.4, nv);
    float edgelight = saturate((nl - nv) * 4) * fresnel;

    //#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
    //#else
    //#endif

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

    specularTerm = saturate((specularTerm - 0.2) / 0.5) * specularTerm; //; + specularTerm;
    //5 * (1 - roughness) * (1 - roughness);
    //specularTerm = pow(specularTerm, 2);

    //specularTerm = pow(lerp(specularTerm, aniso, 1), smoothness * 128);

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

   
    half specColorRaw = specColor;
    // Shift spec color a little bit towards diffuse
    specColor = lerp(diffColor, specColor, 0.9);

    //#if defined(DIRECTIONAL)
    half specularTermModified = 8 * smoothness * smoothness * smoothstep(0,0.1 + (1-smoothness * smoothness) * 0.6,specularTerm);
    specularTerm = lerp(specularTerm, specularTermModified, 1);
    //specularTerm = specularTerm * specularTermModified;

    // Multiply diffuse color by the shadow color in shadowed areas
    //half diffuseTerm = min(diffuseTermRaw / (translucency * 0.25 + 0.1), 1) * 0.8 + nl * 0.2;
    
    //#endif

    half diffuseTermModified = smoothstep(0,0.15,diffuseTerm);
    diffuseTerm = lerp(diffuseTerm, diffuseTermModified, 0.6 * hardness);

    //half diffuseTerm = diffuseTermRaw;
    //diffColor *= lerp(shadowColor, 1, light.color * diffuseTerm);
    //diffColor += shadowColor - light.color * diffuseTerm;

    half fLTDistortion = 2;
    half iLTPower = 3;
    half fLTScale = 2;
    half fLTThickness = 1;

    //Source: https://colinbarrebrisebois.com/2011/03/07/gdc-2011-approximating-translucency-for-a-fast-cheap-and-convincing-subsurface-scattering-look/
    half3 vLTLight = light.dir + normal * fLTDistortion;
    half fLTDot = exp2(saturate(dot(viewDir, -vLTLight)) * iLTPower - iLTPower) * fLTScale;
    half3 fLT = (fLTDot * shadowColor) * fLTThickness;
    //half3 color = shadowColor * saturate(nlUnclamped + 1) * (1 - diffuseTerm) * light.color;

    half3 color = 

#if defined(DIRECTIONAL)
                //diffColor * (gi.diffuse + light.color * lerp(diffuseTerm, 1, pow(translucency, 2.5)) * fLT)
                //diffColor * (gi.diffuse + max(light.color * diffuseTerm, sssTerm))
                //diffColor * (gi.diffuse + light.color * diffuseTerm)

                diffColor * (gi.diffuse + light.color * (diffuseTerm + fLT))
                //diffColor * (gi.diffuse + light.color * (diffuseTerm)) + fLT

#else
                diffColor * (gi.diffuse + light.color * diffuseTerm)
#endif
                //diffColor * (gi.diffuse + light.color * max(diffuseTerm, fLT)) + 

                //+ diffColor * shadowColor * smoothstep(-0.25, 0.25, max(nlUnclamped, nh)) * (1 - diffuseTerm) * translucency * light.color
                + max(edgelight * edgeLightStrength * specColor * (1 + diffColor) * 4, specularTerm * FresnelTerm(specColor, lh)) * light.color
                + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);
    return half4(color, 1);
}