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
half4 CustomLighting (half3 diffColor, half3 shadowColor, half3 specColor, half3 translucency, half edgeLightStrength, half oneMinusReflectivity, half smoothness,
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

    #if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
    #else
    diffuseTerm = smoothstep(0, 0.2, diffuseTerm) * 0.6 + smoothstep(0, light.color, diffuseTerm) * 0.4;

    float fresnel = smoothstep(0.8, 0.2, nv);
    float edgelight = saturate((nl - nv) * 4) * fresnel;
    #endif

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

    specularTerm *= smoothstep(0.4, 1, specularTerm);


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
	/*
    half3 color =   diffColor * (gi.diffuse + light.color * diffuseTerm)
                    + specularTerm * light.color * FresnelTerm (specColor, lh)
                    + surfaceReduction * gi.specular * FresnelLerp (specColor, grazingTerm, nv);
	*/

    half fLTDistortion = 0.5;
    half iLTPower = 12;
    half fLTScale = 1;

    //half3 vLTLight = light.dir + normal.xyz * fLTDistortion;
    //half fLTDot = pow(saturate(dot(viewDir, -vLTLight)), iLTPower) * fLTScale;
    //half3 fLT = fLightAttentuation * (fLTDot + fLTAmbient.rgb) * fLTThickness;
    //half3 color = shadowColor * saturate(nlUnclamped + 1) * (1 - diffuseTerm) * light.color;

    //original
    //float3 H = normalize(light.dir + normal * fLTDistortion);
    //float I = pow(saturate(dot(viewDir, -H)), iLTPower) * fLTScale;

    //float3 H = normalize(light.dir + normal * translucency);
    //float sss = saturate(dot(viewDir, -H));

    half3 color =
#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
                diffColor * (gi.diffuse + light.color * diffuseTerm)
                + specularTerm * light.color * FresnelTerm(specColor, lh)
#else
    //*/
                diffColor * (gi.diffuse + lerp(light.color * diffuseTerm, 1, pow(translucency, 2.5)))
                * lerp(1, shadowColor, smoothstep(0.5, -0.25, min(nlUnclamped, nh) * light.color) * (1 - step(translucency, 0)))
                + diffColor * shadowColor * smoothstep(-0.25, 0.25, max(nlUnclamped, nh)) * (1 - diffuseTerm) * translucency * light.color

    //*/
                //+ diffColor * (fLTDot) * shadowColor
                + max(edgelight * edgeLightStrength * 6 * (specColor + specColor * diffColor * 4), specularTerm * FresnelTerm(specColor, lh)) * light.color
                #endif

                + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, pow(nv, 1 + edgeLightStrength));

    return half4(color, 1);
}