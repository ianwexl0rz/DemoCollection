// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Stochastic (Amplify)"
{
    Properties
    {
		_Color("Color", Color) = (0,0,0,0)
		_MainTexTinput("MainTexTinput", 2D) = "gray" {}
		[NoScaleOffset]_MainTexInvT("MainTexInvT", 2D) = "white" {}
		[NoScaleOffset]_NormalTinput("NormalTinput", 2D) = "gray" {}
		[NoScaleOffset]_NormalInvT("NormalInvT", 2D) = "white" {}
		_MainTexColorSpaceOrigin("MainTexColorSpaceOrigin", Vector) = (0,0,0,0)
		_MainTexColorSpaceVector1("MainTexColorSpaceVector1", Vector) = (0,0,0,0)
		_MainTexColorSpaceVector2("MainTexColorSpaceVector2", Vector) = (0,0,0,0)
		_MainTexColorSpaceVector3("MainTexColorSpaceVector3", Vector) = (0,0,0,0)
		_MainTexDXTScalars("MainTexDXTScalars", Vector) = (1,1,1,0)
		_NormalColorSpaceOrigin("NormalColorSpaceOrigin", Vector) = (0,0,0,0)
		_NormalColorSpaceVector1("NormalColorSpaceVector1", Vector) = (0,0,0,0)
		_NormalColorSpaceVector2("NormalColorSpaceVector2", Vector) = (0,0,0,0)
		_NormalColorSpaceVector3("NormalColorSpaceVector3", Vector) = (0,0,0,0)
		_NormalDXTScalars("NormalDXTScalars", Vector) = (1,1,1,0)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
    }


    SubShader
    {
		
        Tags { "RenderPipeline"="LightweightPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		Cull Back
		HLSLINCLUDE
		#pragma target 3.0
		ENDHLSL
		
        Pass
        {
			
        	Tags { "LightMode"="LightweightForward" }

        	Name "Base"
			Blend One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
            
        	HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            

        	// -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            
        	// -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex vert
        	#pragma fragment frag

        	#define ASE_SRP_VERSION 51300
        	#define _NORMALMAP 1


        	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"

			float4 _Color;
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			sampler2D _MainTexInvT;
			sampler2D _MainTexTinput;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			uniform float4 _MainTexTinput_TexelSize;
			uniform float4 _MainTexInvT_TexelSize;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			sampler2D _NormalInvT;
			sampler2D _NormalTinput;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			uniform float4 _NormalTinput_TexelSize;
			uniform float4 _NormalInvT_TexelSize;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _Smoothness;

            struct GraphVertexInput
            {
                float4 vertex : POSITION;
                float3 ase_normal : NORMAL;
                float4 ase_tangent : TANGENT;
                float4 texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

        	struct GraphVertexOutput
            {
                float4 clipPos                : SV_POSITION;
                float4 lightmapUVOrVertexSH	  : TEXCOORD0;
        		half4 fogFactorAndVertexLight : TEXCOORD1; // x: fogFactor, yzw: vertex light
            	float4 shadowCoord            : TEXCOORD2;
				float4 tSpace0					: TEXCOORD3;
				float4 tSpace1					: TEXCOORD4;
				float4 tSpace2					: TEXCOORD5;
				float4 ase_texcoord7 : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            	UNITY_VERTEX_OUTPUT_STEREO
            };

			
            GraphVertexOutput vert (GraphVertexInput v  )
        	{
        		GraphVertexOutput o = (GraphVertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
            	UNITY_TRANSFER_INSTANCE_ID(v, o);
        		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord7.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord7.zw = 0;
				float3 vertexValue =  float3( 0, 0, 0 ) ;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal =  v.ase_normal ;

        		// Vertex shader outputs defined by graph
                float3 lwWNormal = TransformObjectToWorldNormal(v.ase_normal);
				float3 lwWorldPos = TransformObjectToWorld(v.vertex.xyz);
				float3 lwWTangent = TransformObjectToWorldDir(v.ase_tangent.xyz);
				float3 lwWBinormal = normalize(cross(lwWNormal, lwWTangent) * v.ase_tangent.w);
				o.tSpace0 = float4(lwWTangent.x, lwWBinormal.x, lwWNormal.x, lwWorldPos.x);
				o.tSpace1 = float4(lwWTangent.y, lwWBinormal.y, lwWNormal.y, lwWorldPos.y);
				o.tSpace2 = float4(lwWTangent.z, lwWBinormal.z, lwWNormal.z, lwWorldPos.z);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                
         		// We either sample GI from lightmap or SH.
        	    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
                // see DECLARE_LIGHTMAP_OR_SH macro.
        	    // The following funcions initialize the correct variable with correct data
        	    OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy);
        	    OUTPUT_SH(lwWNormal, o.lightmapUVOrVertexSH.xyz);

        	    half3 vertexLight = VertexLighting(vertexInput.positionWS, lwWNormal);
        	    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
        	    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
        	    o.clipPos = vertexInput.positionCS;

        	#ifdef _MAIN_LIGHT_SHADOWS
        		o.shadowCoord = GetShadowCoord(vertexInput);
        	#endif
        		return o;
        	}

        	half4 frag (GraphVertexOutput IN  ) : SV_Target
            {
            	UNITY_SETUP_INSTANCE_ID(IN);

        		float3 WorldSpaceNormal = normalize(float3(IN.tSpace0.z,IN.tSpace1.z,IN.tSpace2.z));
				float3 WorldSpaceTangent = float3(IN.tSpace0.x,IN.tSpace1.x,IN.tSpace2.x);
				float3 WorldSpaceBiTangent = float3(IN.tSpace0.y,IN.tSpace1.y,IN.tSpace2.y);
				float3 WorldSpacePosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldSpaceViewDirection = SafeNormalize( _WorldSpaceCameraPos.xyz  - WorldSpacePosition );
    
				float2 uv0_MainTexTinput = IN.ase_texcoord7.xy * _MainTexTinput_ST.xy + _MainTexTinput_ST.zw;
				float2 break31_g92 = ( uv0_MainTexTinput * 3.464 );
				float2 appendResult32_g92 = (float2(( break31_g92.x + ( -0.5773503 * break31_g92.y ) ) , ( break31_g92.y * 1.154701 )));
				float2 temp_output_13_0_g92 = frac( appendResult32_g92 );
				float2 break12_g92 = temp_output_13_0_g92;
				float temp_output_3_0_g92 = ( 1.0 - ( break12_g92.x + break12_g92.y ) );
				float3 appendResult38_g92 = (float3(temp_output_13_0_g92 , temp_output_3_0_g92));
				float3 appendResult25_g92 = (float3(( 1.0 - temp_output_13_0_g92 ) , ( temp_output_3_0_g92 * -1.0 )));
				float3 temp_output_26_0_g92 = (( temp_output_3_0_g92 > 0.0 ) ? appendResult38_g92 :  appendResult25_g92 );
				float3 break5_g92 = temp_output_26_0_g92;
				float3 break42_g92 = ( temp_output_26_0_g92 * temp_output_26_0_g92 );
				float4 appendResult45_g92 = (float4(break5_g92.z , break5_g92.y , break5_g92.x , rsqrt( ( break42_g92.x + break42_g92.y + break42_g92.z ) )));
				float4 temp_output_169_0 = appendResult45_g92;
				float4 break135_g101 = temp_output_169_0;
				float2 temp_output_2_0_g95 = ( floor( appendResult32_g92 ) + (( temp_output_3_0_g92 > 0.0 ) ? 0.0 :  1.0 ) );
				float2 break10_g95 = ( float2( 127.1,269.5 ) * temp_output_2_0_g95 );
				float2 break11_g95 = ( float2( 311.7,183.3 ) * temp_output_2_0_g95 );
				float2 appendResult14_g95 = (float2(( break10_g95.x + break10_g95.y ) , ( break11_g95.x + break11_g95.y )));
				float2 temp_output_169_46 = ( frac( ( sin( appendResult14_g95 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_48_0_g101 = ddx( uv0_MainTexTinput );
				float2 temp_output_49_0_g101 = ddy( uv0_MainTexTinput );
				float2 _Vector0 = float2(0,1);
				float2 _Vector1 = float2(1,0);
				float2 temp_output_2_0_g93 = ( floor( appendResult32_g92 ) + (( temp_output_3_0_g92 > 0.0 ) ? _Vector0 :  _Vector1 ) );
				float2 break10_g93 = ( float2( 127.1,269.5 ) * temp_output_2_0_g93 );
				float2 break11_g93 = ( float2( 311.7,183.3 ) * temp_output_2_0_g93 );
				float2 appendResult14_g93 = (float2(( break10_g93.x + break10_g93.y ) , ( break11_g93.x + break11_g93.y )));
				float2 temp_output_169_47 = ( frac( ( sin( appendResult14_g93 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_2_0_g94 = ( floor( appendResult32_g92 ) + (( temp_output_3_0_g92 > 0.0 ) ? _Vector1 :  _Vector0 ) );
				float2 break10_g94 = ( float2( 127.1,269.5 ) * temp_output_2_0_g94 );
				float2 break11_g94 = ( float2( 311.7,183.3 ) * temp_output_2_0_g94 );
				float2 appendResult14_g94 = (float2(( break10_g94.x + break10_g94.y ) , ( break11_g94.x + break11_g94.y )));
				float2 temp_output_169_48 = ( frac( ( sin( appendResult14_g94 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float4 break84_g101 = ( ( break135_g101.w * ( ( tex2D( _MainTexTinput, temp_output_169_46, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.x ) + ( tex2D( _MainTexTinput, temp_output_169_47, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.y ) + ( tex2D( _MainTexTinput, temp_output_169_48, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.z ) + -0.5 ) * float4( _MainTexDXTScalars , 0.0 ) ) + 0.5 );
				float2 appendResult42_g101 = (float2(_MainTexTinput_TexelSize.z , _MainTexTinput_TexelSize.w));
				float2 temp_output_56_0_g101 = ( temp_output_48_0_g101 * appendResult42_g101 );
				float dotResult58_g101 = dot( temp_output_56_0_g101 , temp_output_56_0_g101 );
				float2 temp_output_54_0_g101 = ( temp_output_49_0_g101 * appendResult42_g101 );
				float dotResult60_g101 = dot( temp_output_54_0_g101 , temp_output_54_0_g101 );
				float temp_output_85_0_g101 = ( max( ( log2( max( dotResult58_g101 , dotResult60_g101 ) ) * 0.5 ) , 0.0 ) / _MainTexInvT_TexelSize.w );
				float2 appendResult88_g101 = (float2(break84_g101.r , temp_output_85_0_g101));
				float2 appendResult86_g101 = (float2(break84_g101.g , temp_output_85_0_g101));
				float2 appendResult87_g101 = (float2(0.0 , temp_output_85_0_g101));
				
				float4 break135_g102 = temp_output_169_0;
				float2 uv0_NormalTinput = IN.ase_texcoord7.xy * _NormalTinput_ST.xy + _NormalTinput_ST.zw;
				float2 temp_output_48_0_g102 = ddx( uv0_NormalTinput );
				float2 temp_output_49_0_g102 = ddy( uv0_NormalTinput );
				float4 break84_g102 = ( ( break135_g102.w * ( ( tex2D( _NormalTinput, temp_output_169_46, temp_output_48_0_g102, temp_output_49_0_g102 ) * break135_g102.x ) + ( tex2D( _NormalTinput, temp_output_169_47, temp_output_48_0_g102, temp_output_49_0_g102 ) * break135_g102.y ) + ( tex2D( _NormalTinput, temp_output_169_48, temp_output_48_0_g102, temp_output_49_0_g102 ) * break135_g102.z ) + -0.5 ) * float4( _NormalDXTScalars , 0.0 ) ) + 0.5 );
				float2 appendResult42_g102 = (float2(_NormalTinput_TexelSize.z , _NormalTinput_TexelSize.w));
				float2 temp_output_56_0_g102 = ( temp_output_48_0_g102 * appendResult42_g102 );
				float dotResult58_g102 = dot( temp_output_56_0_g102 , temp_output_56_0_g102 );
				float2 temp_output_54_0_g102 = ( temp_output_49_0_g102 * appendResult42_g102 );
				float dotResult60_g102 = dot( temp_output_54_0_g102 , temp_output_54_0_g102 );
				float temp_output_85_0_g102 = ( max( ( log2( max( dotResult58_g102 , dotResult60_g102 ) ) * 0.5 ) , 0.0 ) / _NormalInvT_TexelSize.w );
				float2 appendResult88_g102 = (float2(break84_g102.r , temp_output_85_0_g102));
				float2 appendResult86_g102 = (float2(break84_g102.g , temp_output_85_0_g102));
				float2 appendResult87_g102 = (float2(0.0 , temp_output_85_0_g102));
				
				
		        float3 Albedo = ( _Color * float4( ( _MainTexColorSpaceOrigin + ( _MainTexColorSpaceVector1 * tex2D( _MainTexInvT, appendResult88_g101 ).r ) + ( _MainTexColorSpaceVector2 * tex2D( _MainTexInvT, appendResult86_g101 ).g ) + ( _MainTexColorSpaceVector3 * tex2D( _MainTexInvT, appendResult87_g101 ).b ) ) , 0.0 ) ).rgb;
				float3 Normal = ( _NormalColorSpaceOrigin + ( _NormalColorSpaceVector1 * tex2D( _NormalInvT, appendResult88_g102 ).r ) + ( _NormalColorSpaceVector2 * tex2D( _NormalInvT, appendResult86_g102 ).g ) + ( _NormalColorSpaceVector3 * tex2D( _NormalInvT, appendResult87_g102 ).b ) );
				float3 Emission = 0;
				float3 Specular = float3(0.5, 0.5, 0.5);
				float Metallic = 0;
				float Smoothness = _Smoothness;
				float Occlusion = 1;
				float Alpha = 1;
				float AlphaClipThreshold = 0;

        		InputData inputData;
        		inputData.positionWS = WorldSpacePosition;

        #ifdef _NORMALMAP
        	    inputData.normalWS = normalize(TransformTangentToWorld(Normal, half3x3(WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal)));
        #else
            #if !SHADER_HINT_NICE_QUALITY
                inputData.normalWS = WorldSpaceNormal;
            #else
        	    inputData.normalWS = normalize(WorldSpaceNormal);
            #endif
        #endif

        #if !SHADER_HINT_NICE_QUALITY
        	    // viewDirection should be normalized here, but we avoid doing it as it's close enough and we save some ALU.
        	    inputData.viewDirectionWS = WorldSpaceViewDirection;
        #else
        	    inputData.viewDirectionWS = normalize(WorldSpaceViewDirection);
        #endif

        	    inputData.shadowCoord = IN.shadowCoord;

        	    inputData.fogCoord = IN.fogFactorAndVertexLight.x;
        	    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
        	    inputData.bakedGI = SAMPLE_GI(IN.lightmapUVOrVertexSH.xy, IN.lightmapUVOrVertexSH.xyz, inputData.normalWS);

        		half4 color = LightweightFragmentPBR(
        			inputData, 
        			Albedo, 
        			Metallic, 
        			Specular, 
        			Smoothness, 
        			Occlusion, 
        			Emission, 
        			Alpha);

			#ifdef TERRAIN_SPLAT_ADDPASS
				color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
			#else
				color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
			#endif

        #if _AlphaClip
        		clip(Alpha - AlphaClipThreshold);
        #endif

		#if ASE_LW_FINAL_COLOR_ALPHA_MULTIPLY
				color.rgb *= color.a;
		#endif
        		return color;
            }

        	ENDHLSL
        }

		
        Pass
        {
			
        	Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #define ASE_SRP_VERSION 51300


            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct GraphVertexInput
            {
                float4 vertex : POSITION;
                float3 ase_normal : NORMAL;
				
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			
        	struct VertexOutput
        	{
        	    float4 clipPos      : SV_POSITION;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
        	};

			
            // x: global clip space bias, y: normal world space bias
            float3 _LightDirection;

            VertexOutput ShadowPassVertex(GraphVertexInput v )
        	{
        	    VertexOutput o;
        	    UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

				
				float3 vertexValue =  float3(0,0,0) ;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal =  v.ase_normal ;

        	    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

                float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
                float scale = invNdotL * _ShadowBias.y;

                // normal bias is negative since we want to apply an inset normal offset
                positionWS = _LightDirection * _ShadowBias.xxx + positionWS;
				positionWS = normalWS * scale.xxx + positionWS;
                float4 clipPos = TransformWorldToHClip(positionWS);

                // _ShadowBias.x sign depens on if platform has reversed z buffer
                //clipPos.z += _ShadowBias.x;

        	#if UNITY_REVERSED_Z
        	    clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
        	#else
        	    clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
        	#endif
                o.clipPos = clipPos;

        	    return o;
        	}

            half4 ShadowPassFragment(VertexOutput IN  ) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);

               

				float Alpha = 1;
				float AlphaClipThreshold = AlphaClipThreshold;

         #if _AlphaClip
        		clip(Alpha - AlphaClipThreshold);
        #endif
                return 0;
            }

            ENDHLSL
        }

		
        Pass
        {
			
        	Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
			ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            #define ASE_SRP_VERSION 51300


            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			
            struct GraphVertexInput
            {
                float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

        	struct VertexOutput
        	{
        	    float4 clipPos      : SV_POSITION;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
        	};

			           

            VertexOutput vert(GraphVertexInput v  )
            {
                VertexOutput o = (VertexOutput)0;
        	    UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				
				float3 vertexValue =  float3(0,0,0) ;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal =  v.ase_normal ;

        	    o.clipPos = TransformObjectToHClip(v.vertex.xyz);
        	    return o;
            }

            half4 frag(VertexOutput IN  ) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);

				

				float Alpha = 1;
				float AlphaClipThreshold = AlphaClipThreshold;

         #if _AlphaClip
        		clip(Alpha - AlphaClipThreshold);
        #endif
                return 0;
            }
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
		
        Pass
        {
			
        	Name "Meta"
            Tags { "LightMode"="Meta" }

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            #define ASE_SRP_VERSION 51300


			uniform float4 _MainTex_ST;
			
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			float4 _Color;
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			sampler2D _MainTexInvT;
			sampler2D _MainTexTinput;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			uniform float4 _MainTexTinput_TexelSize;
			uniform float4 _MainTexInvT_TexelSize;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;

            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature EDITOR_VISUALIZATION


            struct GraphVertexInput
            {
                float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

        	struct VertexOutput
        	{
        	    float4 clipPos      : SV_POSITION;
                float4 ase_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
        	};

			
            VertexOutput vert(GraphVertexInput v  )
            {
                VertexOutput o = (VertexOutput)0;
        	    UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;

				float3 vertexValue =  float3(0,0,0) ;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal =  v.ase_normal ;
				
                o.clipPos = MetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST);
        	    return o;
            }

            half4 frag(VertexOutput IN  ) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);

           		float2 uv0_MainTexTinput = IN.ase_texcoord.xy * _MainTexTinput_ST.xy + _MainTexTinput_ST.zw;
           		float2 break31_g92 = ( uv0_MainTexTinput * 3.464 );
           		float2 appendResult32_g92 = (float2(( break31_g92.x + ( -0.5773503 * break31_g92.y ) ) , ( break31_g92.y * 1.154701 )));
           		float2 temp_output_13_0_g92 = frac( appendResult32_g92 );
           		float2 break12_g92 = temp_output_13_0_g92;
           		float temp_output_3_0_g92 = ( 1.0 - ( break12_g92.x + break12_g92.y ) );
           		float3 appendResult38_g92 = (float3(temp_output_13_0_g92 , temp_output_3_0_g92));
           		float3 appendResult25_g92 = (float3(( 1.0 - temp_output_13_0_g92 ) , ( temp_output_3_0_g92 * -1.0 )));
           		float3 temp_output_26_0_g92 = (( temp_output_3_0_g92 > 0.0 ) ? appendResult38_g92 :  appendResult25_g92 );
           		float3 break5_g92 = temp_output_26_0_g92;
           		float3 break42_g92 = ( temp_output_26_0_g92 * temp_output_26_0_g92 );
           		float4 appendResult45_g92 = (float4(break5_g92.z , break5_g92.y , break5_g92.x , rsqrt( ( break42_g92.x + break42_g92.y + break42_g92.z ) )));
           		float4 temp_output_169_0 = appendResult45_g92;
           		float4 break135_g101 = temp_output_169_0;
           		float2 temp_output_2_0_g95 = ( floor( appendResult32_g92 ) + (( temp_output_3_0_g92 > 0.0 ) ? 0.0 :  1.0 ) );
           		float2 break10_g95 = ( float2( 127.1,269.5 ) * temp_output_2_0_g95 );
           		float2 break11_g95 = ( float2( 311.7,183.3 ) * temp_output_2_0_g95 );
           		float2 appendResult14_g95 = (float2(( break10_g95.x + break10_g95.y ) , ( break11_g95.x + break11_g95.y )));
           		float2 temp_output_169_46 = ( frac( ( sin( appendResult14_g95 ) * 43758.55 ) ) + uv0_MainTexTinput );
           		float2 temp_output_48_0_g101 = ddx( uv0_MainTexTinput );
           		float2 temp_output_49_0_g101 = ddy( uv0_MainTexTinput );
           		float2 _Vector0 = float2(0,1);
           		float2 _Vector1 = float2(1,0);
           		float2 temp_output_2_0_g93 = ( floor( appendResult32_g92 ) + (( temp_output_3_0_g92 > 0.0 ) ? _Vector0 :  _Vector1 ) );
           		float2 break10_g93 = ( float2( 127.1,269.5 ) * temp_output_2_0_g93 );
           		float2 break11_g93 = ( float2( 311.7,183.3 ) * temp_output_2_0_g93 );
           		float2 appendResult14_g93 = (float2(( break10_g93.x + break10_g93.y ) , ( break11_g93.x + break11_g93.y )));
           		float2 temp_output_169_47 = ( frac( ( sin( appendResult14_g93 ) * 43758.55 ) ) + uv0_MainTexTinput );
           		float2 temp_output_2_0_g94 = ( floor( appendResult32_g92 ) + (( temp_output_3_0_g92 > 0.0 ) ? _Vector1 :  _Vector0 ) );
           		float2 break10_g94 = ( float2( 127.1,269.5 ) * temp_output_2_0_g94 );
           		float2 break11_g94 = ( float2( 311.7,183.3 ) * temp_output_2_0_g94 );
           		float2 appendResult14_g94 = (float2(( break10_g94.x + break10_g94.y ) , ( break11_g94.x + break11_g94.y )));
           		float2 temp_output_169_48 = ( frac( ( sin( appendResult14_g94 ) * 43758.55 ) ) + uv0_MainTexTinput );
           		float4 break84_g101 = ( ( break135_g101.w * ( ( tex2D( _MainTexTinput, temp_output_169_46, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.x ) + ( tex2D( _MainTexTinput, temp_output_169_47, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.y ) + ( tex2D( _MainTexTinput, temp_output_169_48, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.z ) + -0.5 ) * float4( _MainTexDXTScalars , 0.0 ) ) + 0.5 );
           		float2 appendResult42_g101 = (float2(_MainTexTinput_TexelSize.z , _MainTexTinput_TexelSize.w));
           		float2 temp_output_56_0_g101 = ( temp_output_48_0_g101 * appendResult42_g101 );
           		float dotResult58_g101 = dot( temp_output_56_0_g101 , temp_output_56_0_g101 );
           		float2 temp_output_54_0_g101 = ( temp_output_49_0_g101 * appendResult42_g101 );
           		float dotResult60_g101 = dot( temp_output_54_0_g101 , temp_output_54_0_g101 );
           		float temp_output_85_0_g101 = ( max( ( log2( max( dotResult58_g101 , dotResult60_g101 ) ) * 0.5 ) , 0.0 ) / _MainTexInvT_TexelSize.w );
           		float2 appendResult88_g101 = (float2(break84_g101.r , temp_output_85_0_g101));
           		float2 appendResult86_g101 = (float2(break84_g101.g , temp_output_85_0_g101));
           		float2 appendResult87_g101 = (float2(0.0 , temp_output_85_0_g101));
           		
				
		        float3 Albedo = ( _Color * float4( ( _MainTexColorSpaceOrigin + ( _MainTexColorSpaceVector1 * tex2D( _MainTexInvT, appendResult88_g101 ).r ) + ( _MainTexColorSpaceVector2 * tex2D( _MainTexInvT, appendResult86_g101 ).g ) + ( _MainTexColorSpaceVector3 * tex2D( _MainTexInvT, appendResult87_g101 ).b ) ) , 0.0 ) ).rgb;
				float3 Emission = 0;
				float Alpha = 1;
				float AlphaClipThreshold = 0;

         #if _AlphaClip
        		clip(Alpha - AlphaClipThreshold);
        #endif

                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = Albedo;
                metaInput.Emission = Emission;
                
                return MetaFragment(metaInput);
            }
            ENDHLSL
        }
		
    }
    FallBack "Hidden/InternalErrorShader"
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=16700
-1937.5;142;1908;1023;-2818.52;184.1788;2.482796;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;79;3824,64;Float;True;Property;_MainTexTinput;MainTexTinput;1;0;Create;True;0;0;False;0;None;27f20a6f68ca6c7489298f1342b8f138;False;gray;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.WireNode;160;4688,16;Float;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TexturePropertyNode;92;4448,64;Float;True;Property;_MainTexInvT;MainTexInvT;2;1;[NoScaleOffset];Create;True;0;0;False;0;None;f737e9a7d609cb448bf9acff0a945d07;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.FunctionNode;169;4176,798.5217;Float;False;StochasticUV;-1;;92;2a9d09a929865a142ba749e628813d62;0;1;53;SAMPLER2D;;False;4;FLOAT2;46;FLOAT2;47;FLOAT2;48;FLOAT4;0
Node;AmplifyShaderEditor.Vector3Node;108;4480,704;Float;False;Property;_MainTexColorSpaceVector3;MainTexColorSpaceVector3;8;0;Create;True;0;0;False;0;0,0,0;-0.117331,-0.24264,0.6186954;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;107;4480,560;Float;False;Property;_MainTexColorSpaceVector2;MainTexColorSpaceVector2;7;0;Create;True;0;0;False;0;0,0,0;-0.3470023,0.3909135,0.08750208;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;109;4480,848;Float;False;Property;_MainTexDXTScalars;MainTexDXTScalars;9;0;Create;True;0;0;False;0;1,1,1;0.6062531,1.886858,1.481807;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;106;4480,416;Float;False;Property;_MainTexColorSpaceVector1;MainTexColorSpaceVector1;6;0;Create;True;0;0;False;0;0,0,0;1.213327,0.9427675,0.599833;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;105;4480,272;Float;False;Property;_MainTexColorSpaceOrigin;MainTexColorSpaceOrigin;5;0;Create;True;0;0;False;0;0,0,0;0.2401372,-0.08902974,-0.3312318;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;179;4896,240;Float;True;StochasticTextureSampler;-1;;101;444ae51c65aa4be4a80703bd19859863;0;11;104;SAMPLER2D;;False;105;SAMPLER2D;;False;139;FLOAT2;0,0;False;141;FLOAT2;0,0;False;142;FLOAT2;0,0;False;138;FLOAT4;0,0,0,0;False;106;FLOAT3;0,0,0;False;107;FLOAT3;0,0,0;False;108;FLOAT3;0,0,0;False;109;FLOAT3;0,0,0;False;110;FLOAT3;0,0,0;False;1;FLOAT3;103
Node;AmplifyShaderEditor.ColorNode;104;5040,32;Float;False;Property;_Color;Color;0;0;Create;True;0;0;False;0;0,0,0,0;0.4339623,0.4339623,0.4339623,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;136;4480,1008;Float;True;Property;_NormalTinput;NormalTinput;3;1;[NoScaleOffset];Create;True;0;0;False;0;None;eab474d4d707863498d4a3b7cd7e2fdc;False;gray;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;128;5648,208;Float;False;Property;_Smoothness;Smoothness;15;0;Create;True;0;0;False;0;0;0.25;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;178;4896,1344;Float;True;StochasticTextureSampler;-1;;102;444ae51c65aa4be4a80703bd19859863;0;11;104;SAMPLER2D;;False;105;SAMPLER2D;;False;139;FLOAT2;0,0;False;141;FLOAT2;0,0;False;142;FLOAT2;0,0;False;138;FLOAT4;0,0,0,0;False;106;FLOAT3;0,0,0;False;107;FLOAT3;0,0,0;False;108;FLOAT3;0,0,0;False;109;FLOAT3;0,0,0;False;110;FLOAT3;0,0,0;False;1;FLOAT3;103
Node;AmplifyShaderEditor.TexturePropertyNode;137;4480,1200;Float;True;Property;_NormalInvT;NormalInvT;4;1;[NoScaleOffset];Create;True;0;0;False;0;None;eaed06054abc59f47875c87934a44d43;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;5392,128;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;141;4480,1680;Float;False;Property;_NormalColorSpaceVector2;NormalColorSpaceVector2;12;0;Create;True;0;0;False;0;0,0,0;-0.7723546,0.8299038,0.02279877;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;142;4480,1392;Float;False;Property;_NormalColorSpaceOrigin;NormalColorSpaceOrigin;10;0;Create;True;0;0;False;0;0,0,0;0.4390354,-0.3666983,0.5481136;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;138;4480,1536;Float;False;Property;_NormalColorSpaceVector1;NormalColorSpaceVector1;11;0;Create;True;0;0;False;0;0,0,0;0.9586893,0.8899831,0.08104445;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;140;4480,1824;Float;False;Property;_NormalColorSpaceVector3;NormalColorSpaceVector3;13;0;Create;True;0;0;False;0;0,0,0;-0.01343385,-0.02415477,0.424165;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;139;3971.027,2059.864;Float;False;Property;_NormalDXTScalars;NormalDXTScalars;14;0;Create;True;0;0;False;0;1,1,1;0.762998,0.8818905,2.352584;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;346,-134;Float;False;False;2;Float;ASEMaterialInspector;0;1;Hidden/Templates/LightWeightSRPPBR;1976390536c6c564abb90fe41f6ee334;True;Meta;0;3;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;;0;0;Standard;0;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;346,-134;Float;False;False;2;Float;ASEMaterialInspector;0;1;Hidden/Templates/LightWeightSRPPBR;1976390536c6c564abb90fe41f6ee334;True;ShadowCaster;0;1;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;346,-134;Float;False;False;2;Float;ASEMaterialInspector;0;1;Hidden/Templates/LightWeightSRPPBR;1976390536c6c564abb90fe41f6ee334;True;DepthOnly;0;2;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;5993,19;Float;False;True;2;Float;ASEMaterialInspector;0;2;Stochastic (Amplify);1976390536c6c564abb90fe41f6ee334;True;Base;0;0;Base;11;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=LightweightForward;False;0;;0;0;Standard;2;Vertex Position,InvertActionOnDeselection;1;Receive Shadows;1;1;_FinalColorxAlpha;0;4;True;True;True;True;False;11;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;9;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT3;0,0,0;False;10;FLOAT3;0,0,0;False;0
WireConnection;160;0;79;0
WireConnection;169;53;79;0
WireConnection;179;104;160;0
WireConnection;179;105;92;0
WireConnection;179;139;169;46
WireConnection;179;141;169;47
WireConnection;179;142;169;48
WireConnection;179;138;169;0
WireConnection;179;106;105;0
WireConnection;179;107;106;0
WireConnection;179;108;107;0
WireConnection;179;109;108;0
WireConnection;179;110;109;0
WireConnection;178;104;136;0
WireConnection;178;105;137;0
WireConnection;178;139;169;46
WireConnection;178;141;169;47
WireConnection;178;142;169;48
WireConnection;178;138;169;0
WireConnection;178;106;142;0
WireConnection;178;107;138;0
WireConnection;178;108;141;0
WireConnection;178;109;140;0
WireConnection;178;110;139;0
WireConnection;103;0;104;0
WireConnection;103;1;179;103
WireConnection;1;0;103;0
WireConnection;1;1;178;103
WireConnection;1;4;128;0
ASEEND*/
//CHKSM=218806B9F89EC665FD645DD92AB832992FDBE6DB