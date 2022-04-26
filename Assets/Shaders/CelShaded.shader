// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Universal Render Pipeline/Cel Shaded"
{
	Properties
	{
		_MainColor("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex("MainTex", 2D) = "white" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_EdgeLight("EdgeLight", Float) = 1
		_BumpMap("Normal", 2D) = "bump" {}
		_BumpScale("NormalScale", Float) = 1
		[PerRendererData]_DamageFlash("DamageFlash", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	SubShader
	{
		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		
		Cull Off
		HLSLINCLUDE
		#pragma target 2.0
		ENDHLSL

		
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
			#define _NORMALMAP 1

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
			
			#pragma multi_compile_instancing


			sampler2D _MainTex;
			sampler2D _BumpMap;
			UNITY_INSTANCING_BUFFER_START(UniversalRenderPipelineCelShaded)
				UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
				UNITY_DEFINE_INSTANCED_PROP(float4, _BumpMap_ST)
				UNITY_DEFINE_INSTANCED_PROP(float, _DamageFlash)
			UNITY_INSTANCING_BUFFER_END(UniversalRenderPipelineCelShaded)
			CBUFFER_START( UnityPerMaterial )
			float4 _MainColor;
			float _BumpScale;
			float _Metallic;
			float _Smoothness;
			float _EdgeLight;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
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
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			VertexOutput vert ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord7.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord7.zw = 0;
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

				float4 _MainTex_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(UniversalRenderPipelineCelShaded,_MainTex_ST);
				float2 uv_MainTex = IN.ase_texcoord7.xy * _MainTex_ST_Instance.xy + _MainTex_ST_Instance.zw;
				float4 temp_output_7_0 = ( _MainColor * tex2D( _MainTex, uv_MainTex ) );
				
				float4 _BumpMap_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(UniversalRenderPipelineCelShaded,_BumpMap_ST);
				float2 uv_BumpMap = IN.ase_texcoord7.xy * _BumpMap_ST_Instance.xy + _BumpMap_ST_Instance.zw;
				
				float4 color287 = IsGammaSpace() ? float4(1,0,0,0) : float4(1,0,0,0);
				float _DamageFlash_Instance = UNITY_ACCESS_INSTANCED_PROP(UniversalRenderPipelineCelShaded,_DamageFlash);
				
				float3 Albedo = temp_output_7_0.rgb;
				float3 Normal = UnpackNormalScale( tex2D( _BumpMap, uv_BumpMap ), _BumpScale );
				float3 Emission = ( color287 * _DamageFlash_Instance ).rgb;
				float3 Specular = 0.5;
				float Metallic = _Metallic;
				float Smoothness = _Smoothness;
				float Occlusion = 1;
				float Alpha = _MainColor.a;
				float AlphaClipThreshold = 0.5;
				float Falloff = 0.1;
				float EdgeLight = _EdgeLight;

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

				half4 color = UniversalFragmentCelShaded(
					inputData, 
					Albedo, 
					Metallic, 
					Specular, 
					Smoothness, 
					Occlusion, 
					Emission, 
					Alpha,
					Falloff,
					EdgeLight);

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

			UNITY_INSTANCING_BUFFER_START(NewAmplifyShader)
			UNITY_INSTANCING_BUFFER_END(NewAmplifyShader)
			CBUFFER_START( UnityPerMaterial )
			float4 _MainColor;
			float _BumpScale;
			float _Metallic;
			float _Smoothness;
			float _EdgeLight;
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

				
				float Alpha = _MainColor.a;
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

			

			UNITY_INSTANCING_BUFFER_START(NewAmplifyShader)
			UNITY_INSTANCING_BUFFER_END(NewAmplifyShader)
			CBUFFER_START( UnityPerMaterial )
			float4 _MainColor;
			float _BumpScale;
			float _Metallic;
			float _Smoothness;
			float _EdgeLight;
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

				
				float Alpha = _MainColor.a;
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

			#pragma multi_compile_instancing


			sampler2D _MainTex;
			UNITY_INSTANCING_BUFFER_START(NewAmplifyShader)
				UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
				UNITY_DEFINE_INSTANCED_PROP(float, _DamageFlash)
			UNITY_INSTANCING_BUFFER_END(NewAmplifyShader)
			CBUFFER_START( UnityPerMaterial )
			float4 _MainColor;
			float _BumpScale;
			float _Metallic;
			float _Smoothness;
			float _EdgeLight;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
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
				float4 clipPos : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				
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

				float4 _MainTex_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(NewAmplifyShader,_MainTex_ST);
				float2 uv_MainTex = IN.ase_texcoord.xy * _MainTex_ST_Instance.xy + _MainTex_ST_Instance.zw;
				float4 temp_output_7_0 = ( _MainColor * tex2D( _MainTex, uv_MainTex ) );
				
				float4 color287 = IsGammaSpace() ? float4(1,0,0,0) : float4(1,0,0,0);
				float _DamageFlash_Instance = UNITY_ACCESS_INSTANCED_PROP(NewAmplifyShader,_DamageFlash);
				
				
				float3 Albedo = temp_output_7_0.rgb;
				float3 Emission = ( color287 * _DamageFlash_Instance ).rgb;
				float Alpha = _MainColor.a;
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
			
			#pragma multi_compile_instancing


			sampler2D _MainTex;
			UNITY_INSTANCING_BUFFER_START(NewAmplifyShader)
				UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
			UNITY_INSTANCING_BUFFER_END(NewAmplifyShader)
			CBUFFER_START( UnityPerMaterial )
			float4 _MainColor;
			float _BumpScale;
			float _Metallic;
			float _Smoothness;
			float _EdgeLight;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				
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
				float4 _MainTex_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(NewAmplifyShader,_MainTex_ST);
				float2 uv_MainTex = IN.ase_texcoord.xy * _MainTex_ST_Instance.xy + _MainTex_ST_Instance.zw;
				float4 temp_output_7_0 = ( _MainColor * tex2D( _MainTex, uv_MainTex ) );
				
				
				float3 Albedo = temp_output_7_0.rgb;
				float Alpha = _MainColor.a;
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
401;826;983;525;788.1588;467.9711;1.3;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;6;-784,-128;Inherit;True;Property;_MainTex;MainTex;1;0;Create;True;0;0;False;0;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;8;-544,-128;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;5;-466,-315;Inherit;False;Property;_MainColor;Main Color;0;0;Create;True;0;0;False;0;0.5,0.5,0.5,1;0.6037736,0.3871343,0.08828763,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-1168,624;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;119;-2096,832;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WireNode;118;80,864;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TexturePropertyNode;18;-784,64;Inherit;True;Property;_BumpMap;Normal;5;0;Create;False;0;0;False;0;None;None;False;bump;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.NormalVertexDataNode;135;32,-752;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;31;-256,480;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;287;-784,336;Inherit;False;Constant;_DamageFlashColor;DamageFlashColor;7;0;Create;True;0;0;False;0;1,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LengthOpNode;110;-1312,624;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;121;-1376,752;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-192,-224;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;89;-1840,576;Inherit;False;True;False;True;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;-1456,608;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.2,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CosOpNode;106;-1024,624;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;290;-144,0;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-736,624;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;92;-1840,656;Inherit;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;125;-560,736;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;108;-1616,608;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;232;304,-752;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IntNode;114;64,432;Inherit;False;Global;_UseCurvature;_UseCurvature;8;0;Create;True;0;0;False;0;0;0;0;1;INT;0
Node;AmplifyShaderEditor.CameraDepthFade;271;304,-512;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;120;-2112,864;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;241;576,-752;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode;22;-2352,480;Inherit;False;1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenParams;272;32,-384;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;107;-896,576;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;278;784,-672;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;17;16,320;Inherit;False;Property;_EdgeLight;EdgeLight;4;0;Create;True;0;0;False;0;1;0.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;30;-592,592;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;115;256,432;Inherit;False;False;5;0;INT;0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;4;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WireNode;279;560,320;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ObjectScaleNode;233;32,-608;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleDivideOpNode;273;304,-624;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;117;64,832;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WorldToObjectTransfNode;25;-128,480;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;29;-432,592;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-736,256;Inherit;False;Property;_BumpScale;NormalScale;6;0;Create;False;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-112,48;Inherit;False;Property;_Metallic;Metallic;3;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-112,240;Inherit;False;Constant;_Falloff;Falloff;5;0;Create;True;0;0;False;0;0.1;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;137;32,-864;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;133;-224,-864;Inherit;False;Constant;_OutlineTint;OutlineTint;8;0;Create;True;0;0;False;0;0.3,0.3,0.3,0;0.4,0.4,0.4,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-112,144;Inherit;False;Property;_Smoothness;Smoothness;2;0;Create;True;0;0;False;0;0;0.66;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;289;-384,304;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;288;-576,416;Inherit;False;InstancedProperty;_DamageFlash;DamageFlash;7;1;[PerRendererData];Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;27;-2096,656;Inherit;False;Global;_CurvatureFocalPoint;_CurvatureFocalPoint;7;0;Create;True;0;0;False;0;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;19;-544,64;Inherit;True;Property;_TextureSample1;Texture Sample 1;6;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;4;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;156;32,-464;Inherit;False;Constant;_OutlineWidth;Outline Width;7;0;Create;True;0;0;False;0;2;0.002;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;291;-128,-80;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ObjectToWorldTransfNode;24;-2064,480;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;283;336,-244;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;10;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;282;336,-244;Float;False;True;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;10;Universal Render Pipeline/Cel Shaded;af8c29dd691e0e14fbfe92fd55725540;True;Forward;0;1;Forward;13;False;False;False;True;2;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;11;Workflow;1;Surface;0;  Blend;0;Two Sided;0;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Vertex Position,InvertActionOnDeselection;1;0;6;False;True;True;True;True;True;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;281;931.3038,-874.2961;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;10;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;Outline;0;0;Outline;7;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;1;False;-1;False;False;False;False;False;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;284;336,-244;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;10;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;286;336,-244;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;10;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;285;336,-244;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;10;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;8;0;6;0
WireConnection;94;0;110;0
WireConnection;119;0;22;0
WireConnection;118;0;120;0
WireConnection;31;0;24;0
WireConnection;31;1;29;0
WireConnection;110;0;109;0
WireConnection;121;0;109;0
WireConnection;7;0;5;0
WireConnection;7;1;8;0
WireConnection;89;0;24;0
WireConnection;109;0;108;0
WireConnection;106;0;94;0
WireConnection;290;0;289;0
WireConnection;105;0;106;0
WireConnection;92;0;27;0
WireConnection;125;0;105;0
WireConnection;108;0;89;0
WireConnection;108;1;92;0
WireConnection;232;0;135;0
WireConnection;232;1;233;0
WireConnection;120;0;22;0
WireConnection;241;0;232;0
WireConnection;241;1;273;0
WireConnection;241;2;271;0
WireConnection;107;0;106;0
WireConnection;278;0;241;0
WireConnection;278;1;279;0
WireConnection;30;0;105;0
WireConnection;115;0;114;0
WireConnection;115;2;25;0
WireConnection;115;3;117;0
WireConnection;115;4;118;0
WireConnection;279;0;115;0
WireConnection;273;0;156;0
WireConnection;273;1;272;2
WireConnection;117;0;119;0
WireConnection;25;0;31;0
WireConnection;29;1;125;0
WireConnection;137;0;133;0
WireConnection;137;1;7;0
WireConnection;289;0;287;0
WireConnection;289;1;288;0
WireConnection;19;0;18;0
WireConnection;19;5;20;0
WireConnection;291;0;19;0
WireConnection;24;0;22;0
WireConnection;282;0;7;0
WireConnection;282;1;291;0
WireConnection;282;2;290;0
WireConnection;282;3;16;0
WireConnection;282;4;15;0
WireConnection;282;6;5;4
WireConnection;282;11;14;0
WireConnection;282;12;17;0
ASEEND*/
//CHKSM=E05B5314087063980391D8034E7D6424B87AC5CF