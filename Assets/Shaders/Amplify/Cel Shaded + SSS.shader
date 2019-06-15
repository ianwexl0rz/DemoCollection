// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Lightweight Render Pipeline/Stylized/Cel Shaded + SSS"
{
    Properties
    {
		[NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
		_Albedo("Albedo", Color) = (1,1,1,0)
		_ShadowFalloff("Shadow Falloff", Range( 0 , 1)) = 0.05
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.3
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_SubsurfaceColor("Subsurface Color", Color) = (1,0,0,0)
		_Thickness("Thickness", Range( 0 , 1)) = 1
		_Ambient("Ambient", Range( 0 , 1)) = 0
		_NormalDistortion("Normal Distortion", Float) = 1
		_Scale("Scale", Float) = 1
		_Power("Power", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
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


        	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#include "Assets/Shaders/SRP/StylizedLighting.hlsl"

			float4 _Albedo;
			sampler2D _MainTex;
			float4 _SubsurfaceColor;
			float _NormalDistortion;
			float _Power;
			float _Scale;
			float _Ambient;
			float _Thickness;
			float _Metallic;
			float _Smoothness;
			float _ShadowFalloff;

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
				float4 ase_texcoord8 : TEXCOORD8;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            	UNITY_VERTEX_OUTPUT_STEREO
            };

			
            GraphVertexOutput vert (GraphVertexInput v  )
        	{
        		GraphVertexOutput o = (GraphVertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
            	UNITY_TRANSFER_INSTANCE_ID(v, o);
        		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VertexPositionInputs ase_vertexInput = GetVertexPositionInputs (v.vertex.xyz);
				#ifdef _MAIN_LIGHT_SHADOWS//ase_lightAtten_vert
				o.ase_texcoord8 = GetShadowCoord( ase_vertexInput );
				#endif//ase_lightAtten_vert
				
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
    
				float2 uv_MainTex4 = IN.ase_texcoord7.xy;
				float4 temp_output_141_0 = ( _Albedo * tex2D( _MainTex, uv_MainTex4 ) );
				
				float dotResult8_g7 = dot( WorldSpaceViewDirection , -( _MainLightPosition.xyz + ( WorldSpaceNormal * _NormalDistortion ) ) );
				float temp_output_26_0_g7 = _Ambient;
				float ase_lightAtten = 0;
				Light ase_lightAtten_mainLight = GetMainLight( IN.ase_texcoord8 );
				ase_lightAtten = ase_lightAtten_mainLight.distanceAttenuation * ase_lightAtten_mainLight.shadowAttenuation;
				#ifdef _ADDITIONAL_LIGHTS//ase_lightAtten_frag
				int ase_lightAtten_pixelLightCount = GetAdditionalLightsCount();
				for (int i = 0; i < ase_lightAtten_pixelLightCount; ++i)
				{//ase_lightAtten_frag
				Light ase_lightAtten_pointLight = GetAdditionalLight( i, WorldSpacePosition );
				ase_lightAtten += ase_lightAtten_pointLight.distanceAttenuation * ase_lightAtten_pointLight.shadowAttenuation;
				}//ase_lightAtten_frag
				#endif//ase_lightAtten_frag
				ase_lightAtten = saturate( ase_lightAtten );
				float3 temp_cast_1 = (ase_lightAtten).xxx;
				float temp_output_2_0_g8 = ( 1.0 - temp_output_26_0_g7 );
				float temp_output_3_0_g8 = ( 1.0 - temp_output_2_0_g8 );
				float3 appendResult7_g8 = (float3(temp_output_3_0_g8 , temp_output_3_0_g8 , temp_output_3_0_g8));
				
				
		        float3 Albedo = temp_output_141_0.rgb;
				float3 Normal = float3(0, 0, 1);
				float3 Emission = ( temp_output_141_0 * _SubsurfaceColor * ( ( pow( saturate( dotResult8_g7 ) , _Power ) * _Scale ) + temp_output_26_0_g7 ) * _Thickness * _MainLightColor * float4( ( ( temp_cast_1 * temp_output_2_0_g8 ) + appendResult7_g8 ) , 0.0 ) ).rgb;
				float3 Specular = float3(0.5, 0.5, 0.5);
				float Metallic = _Metallic;
				float Smoothness = _Smoothness;
				float Occlusion = 1;
				float Alpha = 1;
				float AlphaClipThreshold = 0;
				float Falloff = _ShadowFalloff;

				SSSData sssData;
				sssData.color = _SubsurfaceColor.rgb;
				sssData.thickness = _Thickness;
				sssData.ambient = _Ambient;

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

        		half4 color = LightweightFragmentStylizedSSS(
        			inputData, 
        			Albedo, 
        			Metallic, 
        			Specular, 
        			Smoothness, 
        			Occlusion, 
        			Emission, 
        			Alpha,
					Falloff,
					sssData);

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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT


			uniform float4 _MainTex_ST;
			
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			float4 _Albedo;
			sampler2D _MainTex;
			float4 _SubsurfaceColor;
			float _NormalDistortion;
			float _Power;
			float _Scale;
			float _Ambient;
			float _Thickness;

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
                float4 ase_texcoord1 : TEXCOORD1;
                float4 ase_texcoord2 : TEXCOORD2;
                float4 ase_texcoord3 : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
        	};

			
            VertexOutput vert(GraphVertexInput v  )
            {
                VertexOutput o = (VertexOutput)0;
        	    UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
				o.ase_texcoord1.xyz = ase_worldPos;
				float3 ase_worldNormal = TransformObjectToWorldNormal(v.ase_normal);
				o.ase_texcoord2.xyz = ase_worldNormal;
				VertexPositionInputs ase_vertexInput = GetVertexPositionInputs (v.vertex.xyz);
				#ifdef _MAIN_LIGHT_SHADOWS//ase_lightAtten_vert
				o.ase_texcoord3 = GetShadowCoord( ase_vertexInput );
				#endif//ase_lightAtten_vert
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				o.ase_texcoord1.w = 0;
				o.ase_texcoord2.w = 0;

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

           		float2 uv_MainTex4 = IN.ase_texcoord.xy;
           		float4 temp_output_141_0 = ( _Albedo * tex2D( _MainTex, uv_MainTex4 ) );
           		
           		float3 ase_worldPos = IN.ase_texcoord1.xyz;
           		float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
           		ase_worldViewDir = normalize(ase_worldViewDir);
           		float3 ase_worldNormal = IN.ase_texcoord2.xyz;
           		float dotResult8_g7 = dot( ase_worldViewDir , -( _MainLightPosition.xyz + ( ase_worldNormal * _NormalDistortion ) ) );
           		float temp_output_26_0_g7 = _Ambient;
           		float ase_lightAtten = 0;
           		Light ase_lightAtten_mainLight = GetMainLight( IN.ase_texcoord3 );
           		ase_lightAtten = ase_lightAtten_mainLight.distanceAttenuation * ase_lightAtten_mainLight.shadowAttenuation;
           		#ifdef _ADDITIONAL_LIGHTS//ase_lightAtten_frag
           		int ase_lightAtten_pixelLightCount = GetAdditionalLightsCount();
           		for (int i = 0; i < ase_lightAtten_pixelLightCount; ++i)
           		{//ase_lightAtten_frag
           		Light ase_lightAtten_pointLight = GetAdditionalLight( i, ase_worldPos );
           		ase_lightAtten += ase_lightAtten_pointLight.distanceAttenuation * ase_lightAtten_pointLight.shadowAttenuation;
           		}//ase_lightAtten_frag
           		#endif//ase_lightAtten_frag
           		ase_lightAtten = saturate( ase_lightAtten );
           		float3 temp_cast_1 = (ase_lightAtten).xxx;
           		float temp_output_2_0_g8 = ( 1.0 - temp_output_26_0_g7 );
           		float temp_output_3_0_g8 = ( 1.0 - temp_output_2_0_g8 );
           		float3 appendResult7_g8 = (float3(temp_output_3_0_g8 , temp_output_3_0_g8 , temp_output_3_0_g8));
           		
				
		        float3 Albedo = temp_output_141_0.rgb;
				float3 Emission = ( temp_output_141_0 * _SubsurfaceColor * ( ( pow( saturate( dotResult8_g7 ) , _Power ) * _Scale ) + temp_output_26_0_g7 ) * _Thickness * _MainLightColor * float4( ( ( temp_cast_1 * temp_output_2_0_g8 ) + appendResult7_g8 ) , 0.0 ) ).rgb;
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
-1914;17.5;1908;1012;1474.735;140.839;1.338018;True;False
Node;AmplifyShaderEditor.SamplerNode;4;-864,128;Float;True;Property;_MainTex;MainTex;0;1;[NoScaleOffset];Create;True;0;0;False;0;None;b8d41c09f1f35114695e188eec79904a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;140;-784,-80;Float;False;Property;_Albedo;Albedo;1;0;Create;True;0;0;False;0;1,1,1,0;1,0.955,0.937,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;20;-864,528;Float;False;Property;_NormalDistortion;Normal Distortion;8;0;Create;True;0;0;False;0;1;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-864,688;Float;False;Property;_Scale;Scale;9;0;Create;True;0;0;False;0;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;34;-864,352;Float;False;Property;_SubsurfaceColor;Subsurface Color;5;0;Create;True;0;0;False;0;1,0,0,0;0.664,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;32;-864,768;Float;False;Property;_Thickness;Thickness;6;0;Create;True;0;0;False;0;1;0.3;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;187;-864,848;Float;False;Property;_Ambient;Ambient;7;0;Create;True;0;0;False;0;0;0.2;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;141;-512,16;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-864,608;Float;False;Property;_Power;Power;10;0;Create;True;0;0;False;0;1;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;207;16,80;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;210;-240,832;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-240,384;Float;False;Property;_Metallic;Metallic;4;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;208;-240,672;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-240,464;Float;False;Property;_Smoothness;Smoothness;3;0;Create;True;0;0;False;0;0.3;0.45;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-240,544;Float;False;Property;_ShadowFalloff;Shadow Falloff;2;0;Create;True;0;0;False;0;0.05;0.05;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;209;-240,784;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;217;-240,176;Float;False;Translucency;-1;;7;ba0af5bc7c9e57f4dac30c5577028b3f;0;7;19;COLOR;1,1,1,0;False;20;COLOR;1,0,0,0;False;21;FLOAT;0;False;22;FLOAT;0;False;23;FLOAT;0;False;24;FLOAT;0;False;26;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;202;208,256;Float;False;True;2;Float;ASEMaterialInspector;0;12;Lightweight Render Pipeline/Stylized/Cel Shaded + SSS;8b9f256ee9f233b4884967b1182825ad;True;Base;0;0;Base;15;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=LightweightForward;False;0;;0;0;Standard;2;Vertex Position,InvertActionOnDeselection;1;Receive Shadows;1;1;_FinalColorxAlpha;0;4;True;True;True;True;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;9;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;11;FLOAT;0;False;12;FLOAT3;0,0,0;False;13;FLOAT;0;False;14;FLOAT;0;False;8;FLOAT3;0,0,0;False;10;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;205;312,18;Float;False;False;2;Float;ASEMaterialInspector;0;12;Lightweight Stylized SSS;8b9f256ee9f233b4884967b1182825ad;True;Meta;0;3;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;;0;0;Standard;0;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;204;312,18;Float;False;False;2;Float;ASEMaterialInspector;0;12;Lightweight Stylized SSS;8b9f256ee9f233b4884967b1182825ad;True;DepthOnly;0;2;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;203;312,18;Float;False;False;2;Float;ASEMaterialInspector;0;12;Lightweight Stylized SSS;8b9f256ee9f233b4884967b1182825ad;True;ShadowCaster;0;1;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=LightweightPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;;0;0;Standard;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;0
WireConnection;141;0;140;0
WireConnection;141;1;4;0
WireConnection;207;0;141;0
WireConnection;210;0;187;0
WireConnection;208;0;34;0
WireConnection;209;0;32;0
WireConnection;217;19;141;0
WireConnection;217;20;34;0
WireConnection;217;21;20;0
WireConnection;217;22;26;0
WireConnection;217;23;28;0
WireConnection;217;24;32;0
WireConnection;217;26;187;0
WireConnection;202;0;207;0
WireConnection;202;2;217;0
WireConnection;202;3;129;0
WireConnection;202;4;5;0
WireConnection;202;11;13;0
WireConnection;202;12;208;0
WireConnection;202;13;209;0
WireConnection;202;14;210;0
ASEEND*/
//CHKSM=5D90C9B6AB7CB703F2B7ACFDEC017FC30398644E