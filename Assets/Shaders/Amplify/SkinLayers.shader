// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Universal Render Pipeline/Test"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (0.95,0.4773483,0.2489,0)
		_BlueColor("Blue Color", Color) = (0,0.6079955,1,0)
		_RedColor("Red Color", Color) = (0.4150943,0.06461374,0.06461374,0)
		_BlueMin("Blue Min", Float) = 0
		_BlueMax("Blue Max", Float) = 0
		_RedMin("Red Min", Float) = 0
		_RedMax("Red Max", Float) = 0
		_LipSmoothness("Lip Smoothness", Range( 0 , 1)) = 1
		_Falloff("Falloff", Range( 0 , 1)) = 1
		_SSSColor("SSS Color", Color) = (1,0,0,0)
		_SSSDepth("SSS Depth", Range( 0 , 1)) = 0
		_SSSAmbient("SSS Ambient", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 0

	}

	SubShader
	{
		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		
		Cull Back
		HLSLINCLUDE
		#pragma target 2.0
		ENDHLSL

		
		Pass
		{
			Name "Outline"

			Cull Front
			Blend Off

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex OutlineVertex
			#pragma fragment OutlineFragment


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			

			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColor;
			float4 _BlueColor;
			float _BlueMin;
			float _BlueMax;
			float4 _RedColor;
			float _RedMin;
			float _RedMax;
			float _Smoothness;
			float _LipSmoothness;
			float _Falloff;
			float4 _SSSColor;
			float _SSSDepth;
			float _SSSAmbient;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD0;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			VertexOutput OutlineVertex(VertexInput v )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
				o.clipPos = vertexInput.positionCS;
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif
				return o;
			}

			half4 OutlineFragment(VertexOutput IN ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = float3(0.5, 0.5, 0.5);
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#if _AlphaClip
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef ASE_FOG
					Color = MixFog(Color, IN.fogFactor);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition(IN.clipPos.xyz, unity_LODFade.x);
				#endif

				return half4(Color, Alpha);
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend One Zero , One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#include "Assets/Shaders/SRP/StylizedLighting.hlsl"
			
			

			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColor;
			float4 _BlueColor;
			float _BlueMin;
			float _BlueMax;
			float4 _RedColor;
			float _RedMin;
			float _RedMax;
			float _Smoothness;
			float _LipSmoothness;
			float _Falloff;
			float4 _SSSColor;
			float _SSSDepth;
			float _SSSAmbient;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				DECLARE_LIGHTMAP_OR_SH( lightmapUV, vertexSH, 0 );
				half4 fogFactorAndVertexLight : TEXCOORD1;
				float4 shadowCoord : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			VertexOutput vert ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 lwWNormal = TransformObjectToWorldNormal(v.ase_normal);
				float3 lwWorldPos = TransformObjectToWorld(v.vertex.xyz);
				float3 lwWTangent = TransformObjectToWorldDir(v.ase_tangent.xyz);
				float3 lwWBinormal = normalize(cross(lwWNormal, lwWTangent) * v.ase_tangent.w);
				o.tSpace0 = float4(lwWTangent.x, lwWBinormal.x, lwWNormal.x, lwWorldPos.x);
				o.tSpace1 = float4(lwWTangent.y, lwWBinormal.y, lwWNormal.y, lwWorldPos.y);
				o.tSpace2 = float4(lwWTangent.z, lwWBinormal.z, lwWNormal.z, lwWorldPos.z);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
				
				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUV );
				OUTPUT_SH(lwWNormal, o.vertexSH );

				half3 vertexLight = VertexLighting(vertexInput.positionWS, lwWNormal);
				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( vertexInput.positionCS.z );
				#else
					half fogFactor = 0;
				#endif
				o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				o.clipPos = vertexInput.positionCS;

				#ifdef _MAIN_LIGHT_SHADOWS
					o.shadowCoord = GetShadowCoord(vertexInput);
				#endif
				return o;
			}

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float3 WorldSpaceNormal = normalize(float3(IN.tSpace0.z,IN.tSpace1.z,IN.tSpace2.z));
				float3 WorldSpaceTangent = float3(IN.tSpace0.x,IN.tSpace1.x,IN.tSpace2.x);
				float3 WorldSpaceBiTangent = float3(IN.tSpace0.y,IN.tSpace1.y,IN.tSpace2.y);
				float3 WorldSpacePosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldSpaceViewDirection = _WorldSpaceCameraPos.xyz  - WorldSpacePosition;
	
				#if SHADER_HINT_NICE_QUALITY
					WorldSpaceViewDirection = SafeNormalize( WorldSpaceViewDirection );
				#endif

				float temp_output_21_0 = saturate( (0.0 + (IN.ase_color.b - _BlueMin) * (1.0 - 0.0) / (_BlueMax - _BlueMin)) );
				float4 lerpResult8 = lerp( _BaseColor , _BlueColor , temp_output_21_0);
				float temp_output_22_0 = saturate( (0.0 + (IN.ase_color.r - _RedMin) * (1.0 - 0.0) / (_RedMax - _RedMin)) );
				float4 lerpResult10 = lerp( lerpResult8 , _RedColor , temp_output_22_0);
				
				float lerpResult33 = lerp( _Smoothness , _LipSmoothness , temp_output_22_0);
				
				float3 Albedo = lerpResult10.rgb;
				float3 Normal = float3(0, 0, 1);
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = 0;
				float Smoothness = lerpResult33;
				float Occlusion = 1;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float Falloff = _Falloff;
				float EdgeLight = 1;

				SSSData sssData;
				sssData.color = ( ( 1.0 - ( temp_output_21_0 * temp_output_22_0 ) ) * _SSSColor ).rgb;
				sssData.thickness = _SSSDepth;
				sssData.ambient = _SSSAmbient;

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

				inputData.viewDirectionWS = WorldSpaceViewDirection;
				inputData.shadowCoord = IN.shadowCoord;

				#ifdef ASE_FOG
					inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				#endif

				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				inputData.bakedGI = SAMPLE_GI( IN.lightmapUV, IN.vertexSH, inputData.normalWS );

				half4 color = UniversalFragmentCelShadedSSS(
					inputData, 
					Albedo, 
					Metallic, 
					Specular, 
					Smoothness, 
					Occlusion, 
					Emission, 
					Alpha,
					Falloff,
					EdgeLight,
					sssData);

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
					#endif
				#endif
				
				#if _AlphaClip
					clip(Alpha - AlphaClipThreshold);
				#endif
				
				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColor;
			float4 _BlueColor;
			float _BlueMin;
			float _BlueMax;
			float4 _RedColor;
			float _RedMin;
			float _RedMax;
			float _Smoothness;
			float _LipSmoothness;
			float _Falloff;
			float4 _SSSColor;
			float _SSSDepth;
			float _SSSAmbient;
			CBUFFER_END


			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			
			float3 _LightDirection;

			VertexOutput ShadowPassVertex( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
				float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

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
				UNITY_SETUP_INSTANCE_ID( IN );

				
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#if _AlphaClip
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColor;
			float4 _BlueColor;
			float _BlueMin;
			float _BlueMax;
			float4 _RedColor;
			float _RedMin;
			float _RedMax;
			float _Smoothness;
			float _LipSmoothness;
			float _Falloff;
			float4 _SSSColor;
			float _SSSDepth;
			float _SSSAmbient;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				o.clipPos = TransformObjectToHClip(v.vertex.xyz);
				return o;
			}

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#if _AlphaClip
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColor;
			float4 _BlueColor;
			float _BlueMin;
			float _BlueMax;
			float4 _RedColor;
			float _RedMin;
			float _RedMax;
			float _Smoothness;
			float _LipSmoothness;
			float _Falloff;
			float4 _SSSColor;
			float _SSSDepth;
			float _SSSAmbient;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_color = v.ase_color;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				o.clipPos = MetaVertexPosition( v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
				return o;
			}

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				float temp_output_21_0 = saturate( (0.0 + (IN.ase_color.b - _BlueMin) * (1.0 - 0.0) / (_BlueMax - _BlueMin)) );
				float4 lerpResult8 = lerp( _BaseColor , _BlueColor , temp_output_21_0);
				float temp_output_22_0 = saturate( (0.0 + (IN.ase_color.r - _RedMin) * (1.0 - 0.0) / (_RedMax - _RedMin)) );
				float4 lerpResult10 = lerp( lerpResult8 , _RedColor , temp_output_22_0);
				
				
				float3 Albedo = lerpResult10.rgb;
				float3 Emission = 0;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

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

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend One Zero , One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_SRP_VERSION 999999

			#pragma enable_d3d11_debug_symbols
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			

			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColor;
			float4 _BlueColor;
			float _BlueMin;
			float _BlueMax;
			float4 _RedColor;
			float _RedMin;
			float _RedMax;
			float _Smoothness;
			float _LipSmoothness;
			float _Falloff;
			float4 _SSSColor;
			float _SSSDepth;
			float _SSSAmbient;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_color : COLOR;
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				o.ase_color = v.ase_color;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( v.vertex.xyz );
				o.clipPos = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				float temp_output_21_0 = saturate( (0.0 + (IN.ase_color.b - _BlueMin) * (1.0 - 0.0) / (_BlueMax - _BlueMin)) );
				float4 lerpResult8 = lerp( _BaseColor , _BlueColor , temp_output_21_0);
				float temp_output_22_0 = saturate( (0.0 + (IN.ase_color.r - _RedMin) * (1.0 - 0.0) / (_RedMax - _RedMin)) );
				float4 lerpResult10 = lerp( lerpResult8 , _RedColor , temp_output_22_0);
				
				
				float3 Albedo = lerpResult10.rgb;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				half4 color = half4( Albedo, Alpha );

				#if _AlphaClip
					clip(Alpha - AlphaClipThreshold);
				#endif

				return color;
			}
			ENDHLSL
		}
		
	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=17200
290;1045;1601;324;-56.38002;417.4242;1.421703;True;True
Node;AmplifyShaderEditor.RangedFloatNode;15;-1530.346,-401.3888;Inherit;False;Property;_BlueMax;Blue Max;4;0;Create;True;0;0;False;0;0;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-1595.131,-489.3885;Inherit;False;Property;_BlueMin;Blue Min;3;0;Create;True;0;0;False;0;0;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;7;-1875.717,-186.7088;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;-775.6785,-19.93043;Inherit;False;Property;_RedMin;Red Min;5;0;Create;True;0;0;False;0;0;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-785.964,93.16989;Inherit;False;Property;_RedMax;Red Max;6;0;Create;True;0;0;False;0;0;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;13;-1257.319,-409.3245;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-1391.858,-972.1507;Inherit;False;Property;_BaseColor;Base Color;0;0;Create;True;0;0;False;0;0.95,0.4773483,0.2489,0;0.759,0.4952818,0.3445859,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;21;-1032.958,-430.2162;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;9;-1405.685,-777.4721;Inherit;False;Property;_BlueColor;Blue Color;1;0;Create;True;0;0;False;0;0,0.6079955,1,0;0.08448491,0.1807424,0.2769999,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;17;-572.9316,-159.0499;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;12;-567.0924,-510.628;Inherit;False;Property;_RedColor;Red Color;2;0;Create;True;0;0;False;0;0.4150943,0.06461374,0.06461374,0;0.582,0.2847532,0.202536,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;8;-891.6846,-740.472;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;22;-309.8888,-156.6645;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;17.27081,64.80875;Inherit;False;Property;_LipSmoothness;Lip Smoothness;7;0;Create;True;0;0;False;0;1;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;33;417.0042,-147.8119;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-19.79601,-181.6132;Inherit;False;Property;_Smoothness;Smoothness;12;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;465.1041,311.0869;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;29;236.284,339.1067;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;456.7071,684.6518;Inherit;False;Property;_SSSDepth;SSS Depth;10;0;Create;True;0;0;False;0;0;0.107;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;10;-159.9242,-520.4476;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;25;147.9315,186.3093;Inherit;False;Property;_Falloff;Falloff;8;0;Create;True;0;0;False;0;1;0.3;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;26;247.4073,490.9517;Inherit;False;Property;_SSSColor;SSS Color;9;0;Create;True;0;0;False;0;1,0,0,0;1,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;28;459.0486,770.6888;Inherit;False;Property;_SSSAmbient;SSS Ambient;11;0;Create;True;0;0;False;0;0;0.929;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-146.647,153.1194;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;911.3205,-178.5735;Float;False;True;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;11;Universal Render Pipeline/Test;e92dacfbb76aca84a9cf42cd8b5228fc;True;Forward;0;1;Forward;16;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;11;Workflow;1;Surface;0;  Blend;0;Two Sided;1;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Vertex Position,InvertActionOnDeselection;1;0;6;True;True;True;True;True;True;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;5;0,0;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;11;New Amplify Shader;e92dacfbb76aca84a9cf42cd8b5228fc;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;11;New Amplify Shader;e92dacfbb76aca84a9cf42cd8b5228fc;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;11;New Amplify Shader;e92dacfbb76aca84a9cf42cd8b5228fc;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;867.2844,-576.6159;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;11;New Amplify Shader;e92dacfbb76aca84a9cf42cd8b5228fc;True;Outline;0;0;Outline;7;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;1;False;-1;False;False;False;False;False;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;11;New Amplify Shader;e92dacfbb76aca84a9cf42cd8b5228fc;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;13;0;7;3
WireConnection;13;1;14;0
WireConnection;13;2;15;0
WireConnection;21;0;13;0
WireConnection;17;0;7;1
WireConnection;17;1;18;0
WireConnection;17;2;19;0
WireConnection;8;0;6;0
WireConnection;8;1;9;0
WireConnection;8;2;21;0
WireConnection;22;0;17;0
WireConnection;33;0;32;0
WireConnection;33;1;24;0
WireConnection;33;2;22;0
WireConnection;31;0;29;0
WireConnection;31;1;26;0
WireConnection;29;0;30;0
WireConnection;10;0;8;0
WireConnection;10;1;12;0
WireConnection;10;2;22;0
WireConnection;30;0;21;0
WireConnection;30;1;22;0
WireConnection;1;0;10;0
WireConnection;1;4;33;0
WireConnection;1;11;25;0
WireConnection;1;13;31;0
WireConnection;1;14;27;0
WireConnection;1;15;28;0
ASEEND*/
//CHKSM=28D723CBA18CEC036E06C81193F762229F5AB354