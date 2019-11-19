// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Stochastic Cel Shaded"
{
	Properties
	{
		_Color("Color", Color) = (0,0,0,0)
		_MainTexTinput("MainTexTinput", 2D) = "gray" {}
		[NoScaleOffset]_MainTexInvT("MainTexInvT", 2D) = "white" {}
		[NoScaleOffset]_NormalTinput("NormalTinput", 2D) = "bump" {}
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
		_NormalScale("Normal Scale", Float) = 4.14
		_Falloff("Falloff", Float) = 0.1
		_EdgeLight("Edge Light", Float) = 0

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
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float4 _Color;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _NormalScale;
			float _Smoothness;
			float _Falloff;
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
			
			

			sampler2D _MainTexInvT;
			sampler2D _MainTexTinput;
			uniform float4 _MainTexTinput_TexelSize;
			uniform float4 _MainTexInvT_TexelSize;
			sampler2D _NormalInvT;
			sampler2D _NormalTinput;
			uniform float4 _NormalTinput_TexelSize;
			uniform float4 _NormalInvT_TexelSize;
			CBUFFER_START( UnityPerMaterial )
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float4 _Color;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _NormalScale;
			float _Smoothness;
			float _Falloff;
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

				float2 uv0_MainTexTinput = IN.ase_texcoord7.xy * _MainTexTinput_ST.xy + _MainTexTinput_ST.zw;
				float2 break31_g1 = ( uv0_MainTexTinput * 3.464 );
				float2 appendResult32_g1 = (float2(( break31_g1.x + ( -0.5773503 * break31_g1.y ) ) , ( break31_g1.y * 1.154701 )));
				float2 temp_output_13_0_g1 = frac( appendResult32_g1 );
				float2 break12_g1 = temp_output_13_0_g1;
				float temp_output_3_0_g1 = ( 1.0 - ( break12_g1.x + break12_g1.y ) );
				float3 appendResult38_g1 = (float3(temp_output_13_0_g1 , temp_output_3_0_g1));
				float3 appendResult25_g1 = (float3(( 1.0 - temp_output_13_0_g1 ) , -temp_output_3_0_g1));
				float3 temp_output_26_0_g1 = (( temp_output_3_0_g1 > 0.0 ) ? appendResult38_g1 :  appendResult25_g1 );
				float3 break42_g1 = ( temp_output_26_0_g1 * temp_output_26_0_g1 );
				float4 appendResult55_g1 = (float4((temp_output_26_0_g1).zyx , rsqrt( ( break42_g1.x + break42_g1.y + break42_g1.z ) )));
				float4 temp_output_181_0 = appendResult55_g1;
				float4 break135_g101 = temp_output_181_0;
				float2 temp_output_2_0_g19 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? 0.0 :  1.0 ) );
				float2 break10_g19 = ( float2( 127.1,269.5 ) * temp_output_2_0_g19 );
				float2 break11_g19 = ( float2( 311.7,183.3 ) * temp_output_2_0_g19 );
				float2 appendResult14_g19 = (float2(( break10_g19.x + break10_g19.y ) , ( break11_g19.x + break11_g19.y )));
				float2 temp_output_181_46 = ( frac( ( sin( appendResult14_g19 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_48_0_g101 = ddx( uv0_MainTexTinput );
				float2 temp_output_49_0_g101 = ddy( uv0_MainTexTinput );
				float2 _Vector0 = float2(0,1);
				float2 _Vector1 = float2(1,0);
				float2 temp_output_2_0_g20 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? _Vector0 :  _Vector1 ) );
				float2 break10_g20 = ( float2( 127.1,269.5 ) * temp_output_2_0_g20 );
				float2 break11_g20 = ( float2( 311.7,183.3 ) * temp_output_2_0_g20 );
				float2 appendResult14_g20 = (float2(( break10_g20.x + break10_g20.y ) , ( break11_g20.x + break11_g20.y )));
				float2 temp_output_181_47 = ( frac( ( sin( appendResult14_g20 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_2_0_g18 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? _Vector1 :  _Vector0 ) );
				float2 break10_g18 = ( float2( 127.1,269.5 ) * temp_output_2_0_g18 );
				float2 break11_g18 = ( float2( 311.7,183.3 ) * temp_output_2_0_g18 );
				float2 appendResult14_g18 = (float2(( break10_g18.x + break10_g18.y ) , ( break11_g18.x + break11_g18.y )));
				float2 temp_output_181_48 = ( frac( ( sin( appendResult14_g18 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float4 break84_g101 = ( ( break135_g101.w * ( ( tex2D( _MainTexTinput, temp_output_181_46, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.x ) + ( tex2D( _MainTexTinput, temp_output_181_47, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.y ) + ( tex2D( _MainTexTinput, temp_output_181_48, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.z ) + -0.5 ) * float4( _MainTexDXTScalars , 0.0 ) ) + 0.5 );
				float2 appendResult42_g101 = (float2(_MainTexTinput_TexelSize.z , _MainTexTinput_TexelSize.w));
				float2 temp_output_56_0_g101 = ( temp_output_48_0_g101 * appendResult42_g101 );
				float dotResult58_g101 = dot( temp_output_56_0_g101 , temp_output_56_0_g101 );
				float2 temp_output_54_0_g101 = ( temp_output_49_0_g101 * appendResult42_g101 );
				float dotResult60_g101 = dot( temp_output_54_0_g101 , temp_output_54_0_g101 );
				float temp_output_85_0_g101 = ( max( ( log2( max( dotResult58_g101 , dotResult60_g101 ) ) * 0.5 ) , 0.0 ) / _MainTexInvT_TexelSize.w );
				float2 appendResult88_g101 = (float2(break84_g101.r , temp_output_85_0_g101));
				float2 appendResult86_g101 = (float2(break84_g101.g , temp_output_85_0_g101));
				float2 appendResult87_g101 = (float2(0.0 , temp_output_85_0_g101));
				
				float4 break135_g102 = temp_output_181_0;
				float2 uv0_NormalTinput = IN.ase_texcoord7.xy * _NormalTinput_ST.xy + _NormalTinput_ST.zw;
				float2 temp_output_48_0_g102 = ddx( uv0_NormalTinput );
				float2 temp_output_49_0_g102 = ddy( uv0_NormalTinput );
				float4 break84_g102 = ( ( break135_g102.w * ( ( tex2D( _NormalTinput, temp_output_181_46, temp_output_48_0_g102, temp_output_49_0_g102 ) * break135_g102.x ) + ( tex2D( _NormalTinput, temp_output_181_47, temp_output_48_0_g102, temp_output_49_0_g102 ) * break135_g102.y ) + ( tex2D( _NormalTinput, temp_output_181_48, temp_output_48_0_g102, temp_output_49_0_g102 ) * break135_g102.z ) + -0.5 ) * float4( _NormalDXTScalars , 0.0 ) ) + 0.5 );
				float2 appendResult42_g102 = (float2(_NormalTinput_TexelSize.z , _NormalTinput_TexelSize.w));
				float2 temp_output_56_0_g102 = ( temp_output_48_0_g102 * appendResult42_g102 );
				float dotResult58_g102 = dot( temp_output_56_0_g102 , temp_output_56_0_g102 );
				float2 temp_output_54_0_g102 = ( temp_output_49_0_g102 * appendResult42_g102 );
				float dotResult60_g102 = dot( temp_output_54_0_g102 , temp_output_54_0_g102 );
				float temp_output_85_0_g102 = ( max( ( log2( max( dotResult58_g102 , dotResult60_g102 ) ) * 0.5 ) , 0.0 ) / _NormalInvT_TexelSize.w );
				float2 appendResult88_g102 = (float2(break84_g102.r , temp_output_85_0_g102));
				float2 appendResult86_g102 = (float2(break84_g102.g , temp_output_85_0_g102));
				float2 appendResult87_g102 = (float2(0.0 , temp_output_85_0_g102));
				float3 break206 = ( _NormalColorSpaceOrigin + ( _NormalColorSpaceVector1 * tex2D( _NormalInvT, appendResult88_g102 ).r ) + ( _NormalColorSpaceVector2 * tex2D( _NormalInvT, appendResult86_g102 ).g ) + ( _NormalColorSpaceVector3 * tex2D( _NormalInvT, appendResult87_g102 ).b ) );
				float4 appendResult207 = (float4(1.0 , break206.y , 0.0 , break206.x));
				
				float3 Albedo = ( float4( ( _MainTexColorSpaceOrigin + ( _MainTexColorSpaceVector1 * tex2D( _MainTexInvT, appendResult88_g101 ).r ) + ( _MainTexColorSpaceVector2 * tex2D( _MainTexInvT, appendResult86_g101 ).g ) + ( _MainTexColorSpaceVector3 * tex2D( _MainTexInvT, appendResult87_g101 ).b ) ) , 0.0 ) * _Color ).rgb;
				float3 Normal = UnpackNormalScale( appendResult207, _NormalScale );
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = 0;
				float Smoothness = _Smoothness;
				float Occlusion = 1;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float Falloff = _Falloff;
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

			CBUFFER_START( UnityPerMaterial )
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float4 _Color;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _NormalScale;
			float _Smoothness;
			float _Falloff;
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
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float4 _Color;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _NormalScale;
			float _Smoothness;
			float _Falloff;
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

			

			sampler2D _MainTexInvT;
			sampler2D _MainTexTinput;
			uniform float4 _MainTexTinput_TexelSize;
			uniform float4 _MainTexInvT_TexelSize;
			CBUFFER_START( UnityPerMaterial )
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float4 _Color;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _NormalScale;
			float _Smoothness;
			float _Falloff;
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

				float2 uv0_MainTexTinput = IN.ase_texcoord.xy * _MainTexTinput_ST.xy + _MainTexTinput_ST.zw;
				float2 break31_g1 = ( uv0_MainTexTinput * 3.464 );
				float2 appendResult32_g1 = (float2(( break31_g1.x + ( -0.5773503 * break31_g1.y ) ) , ( break31_g1.y * 1.154701 )));
				float2 temp_output_13_0_g1 = frac( appendResult32_g1 );
				float2 break12_g1 = temp_output_13_0_g1;
				float temp_output_3_0_g1 = ( 1.0 - ( break12_g1.x + break12_g1.y ) );
				float3 appendResult38_g1 = (float3(temp_output_13_0_g1 , temp_output_3_0_g1));
				float3 appendResult25_g1 = (float3(( 1.0 - temp_output_13_0_g1 ) , -temp_output_3_0_g1));
				float3 temp_output_26_0_g1 = (( temp_output_3_0_g1 > 0.0 ) ? appendResult38_g1 :  appendResult25_g1 );
				float3 break42_g1 = ( temp_output_26_0_g1 * temp_output_26_0_g1 );
				float4 appendResult55_g1 = (float4((temp_output_26_0_g1).zyx , rsqrt( ( break42_g1.x + break42_g1.y + break42_g1.z ) )));
				float4 temp_output_181_0 = appendResult55_g1;
				float4 break135_g101 = temp_output_181_0;
				float2 temp_output_2_0_g19 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? 0.0 :  1.0 ) );
				float2 break10_g19 = ( float2( 127.1,269.5 ) * temp_output_2_0_g19 );
				float2 break11_g19 = ( float2( 311.7,183.3 ) * temp_output_2_0_g19 );
				float2 appendResult14_g19 = (float2(( break10_g19.x + break10_g19.y ) , ( break11_g19.x + break11_g19.y )));
				float2 temp_output_181_46 = ( frac( ( sin( appendResult14_g19 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_48_0_g101 = ddx( uv0_MainTexTinput );
				float2 temp_output_49_0_g101 = ddy( uv0_MainTexTinput );
				float2 _Vector0 = float2(0,1);
				float2 _Vector1 = float2(1,0);
				float2 temp_output_2_0_g20 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? _Vector0 :  _Vector1 ) );
				float2 break10_g20 = ( float2( 127.1,269.5 ) * temp_output_2_0_g20 );
				float2 break11_g20 = ( float2( 311.7,183.3 ) * temp_output_2_0_g20 );
				float2 appendResult14_g20 = (float2(( break10_g20.x + break10_g20.y ) , ( break11_g20.x + break11_g20.y )));
				float2 temp_output_181_47 = ( frac( ( sin( appendResult14_g20 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_2_0_g18 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? _Vector1 :  _Vector0 ) );
				float2 break10_g18 = ( float2( 127.1,269.5 ) * temp_output_2_0_g18 );
				float2 break11_g18 = ( float2( 311.7,183.3 ) * temp_output_2_0_g18 );
				float2 appendResult14_g18 = (float2(( break10_g18.x + break10_g18.y ) , ( break11_g18.x + break11_g18.y )));
				float2 temp_output_181_48 = ( frac( ( sin( appendResult14_g18 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float4 break84_g101 = ( ( break135_g101.w * ( ( tex2D( _MainTexTinput, temp_output_181_46, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.x ) + ( tex2D( _MainTexTinput, temp_output_181_47, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.y ) + ( tex2D( _MainTexTinput, temp_output_181_48, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.z ) + -0.5 ) * float4( _MainTexDXTScalars , 0.0 ) ) + 0.5 );
				float2 appendResult42_g101 = (float2(_MainTexTinput_TexelSize.z , _MainTexTinput_TexelSize.w));
				float2 temp_output_56_0_g101 = ( temp_output_48_0_g101 * appendResult42_g101 );
				float dotResult58_g101 = dot( temp_output_56_0_g101 , temp_output_56_0_g101 );
				float2 temp_output_54_0_g101 = ( temp_output_49_0_g101 * appendResult42_g101 );
				float dotResult60_g101 = dot( temp_output_54_0_g101 , temp_output_54_0_g101 );
				float temp_output_85_0_g101 = ( max( ( log2( max( dotResult58_g101 , dotResult60_g101 ) ) * 0.5 ) , 0.0 ) / _MainTexInvT_TexelSize.w );
				float2 appendResult88_g101 = (float2(break84_g101.r , temp_output_85_0_g101));
				float2 appendResult86_g101 = (float2(break84_g101.g , temp_output_85_0_g101));
				float2 appendResult87_g101 = (float2(0.0 , temp_output_85_0_g101));
				
				
				float3 Albedo = ( float4( ( _MainTexColorSpaceOrigin + ( _MainTexColorSpaceVector1 * tex2D( _MainTexInvT, appendResult88_g101 ).r ) + ( _MainTexColorSpaceVector2 * tex2D( _MainTexInvT, appendResult86_g101 ).g ) + ( _MainTexColorSpaceVector3 * tex2D( _MainTexInvT, appendResult87_g101 ).b ) ) , 0.0 ) * _Color ).rgb;
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
			
			

			sampler2D _MainTexInvT;
			sampler2D _MainTexTinput;
			uniform float4 _MainTexTinput_TexelSize;
			uniform float4 _MainTexInvT_TexelSize;
			CBUFFER_START( UnityPerMaterial )
			float3 _MainTexColorSpaceOrigin;
			float3 _MainTexColorSpaceVector1;
			float4 _MainTexTinput_ST;
			float3 _MainTexDXTScalars;
			float3 _MainTexColorSpaceVector2;
			float3 _MainTexColorSpaceVector3;
			float4 _Color;
			float3 _NormalColorSpaceOrigin;
			float3 _NormalColorSpaceVector1;
			float4 _NormalTinput_ST;
			float3 _NormalDXTScalars;
			float3 _NormalColorSpaceVector2;
			float3 _NormalColorSpaceVector3;
			float _NormalScale;
			float _Smoothness;
			float _Falloff;
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
				float2 uv0_MainTexTinput = IN.ase_texcoord.xy * _MainTexTinput_ST.xy + _MainTexTinput_ST.zw;
				float2 break31_g1 = ( uv0_MainTexTinput * 3.464 );
				float2 appendResult32_g1 = (float2(( break31_g1.x + ( -0.5773503 * break31_g1.y ) ) , ( break31_g1.y * 1.154701 )));
				float2 temp_output_13_0_g1 = frac( appendResult32_g1 );
				float2 break12_g1 = temp_output_13_0_g1;
				float temp_output_3_0_g1 = ( 1.0 - ( break12_g1.x + break12_g1.y ) );
				float3 appendResult38_g1 = (float3(temp_output_13_0_g1 , temp_output_3_0_g1));
				float3 appendResult25_g1 = (float3(( 1.0 - temp_output_13_0_g1 ) , -temp_output_3_0_g1));
				float3 temp_output_26_0_g1 = (( temp_output_3_0_g1 > 0.0 ) ? appendResult38_g1 :  appendResult25_g1 );
				float3 break42_g1 = ( temp_output_26_0_g1 * temp_output_26_0_g1 );
				float4 appendResult55_g1 = (float4((temp_output_26_0_g1).zyx , rsqrt( ( break42_g1.x + break42_g1.y + break42_g1.z ) )));
				float4 temp_output_181_0 = appendResult55_g1;
				float4 break135_g101 = temp_output_181_0;
				float2 temp_output_2_0_g19 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? 0.0 :  1.0 ) );
				float2 break10_g19 = ( float2( 127.1,269.5 ) * temp_output_2_0_g19 );
				float2 break11_g19 = ( float2( 311.7,183.3 ) * temp_output_2_0_g19 );
				float2 appendResult14_g19 = (float2(( break10_g19.x + break10_g19.y ) , ( break11_g19.x + break11_g19.y )));
				float2 temp_output_181_46 = ( frac( ( sin( appendResult14_g19 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_48_0_g101 = ddx( uv0_MainTexTinput );
				float2 temp_output_49_0_g101 = ddy( uv0_MainTexTinput );
				float2 _Vector0 = float2(0,1);
				float2 _Vector1 = float2(1,0);
				float2 temp_output_2_0_g20 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? _Vector0 :  _Vector1 ) );
				float2 break10_g20 = ( float2( 127.1,269.5 ) * temp_output_2_0_g20 );
				float2 break11_g20 = ( float2( 311.7,183.3 ) * temp_output_2_0_g20 );
				float2 appendResult14_g20 = (float2(( break10_g20.x + break10_g20.y ) , ( break11_g20.x + break11_g20.y )));
				float2 temp_output_181_47 = ( frac( ( sin( appendResult14_g20 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float2 temp_output_2_0_g18 = ( floor( appendResult32_g1 ) + (( temp_output_3_0_g1 > 0.0 ) ? _Vector1 :  _Vector0 ) );
				float2 break10_g18 = ( float2( 127.1,269.5 ) * temp_output_2_0_g18 );
				float2 break11_g18 = ( float2( 311.7,183.3 ) * temp_output_2_0_g18 );
				float2 appendResult14_g18 = (float2(( break10_g18.x + break10_g18.y ) , ( break11_g18.x + break11_g18.y )));
				float2 temp_output_181_48 = ( frac( ( sin( appendResult14_g18 ) * 43758.55 ) ) + uv0_MainTexTinput );
				float4 break84_g101 = ( ( break135_g101.w * ( ( tex2D( _MainTexTinput, temp_output_181_46, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.x ) + ( tex2D( _MainTexTinput, temp_output_181_47, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.y ) + ( tex2D( _MainTexTinput, temp_output_181_48, temp_output_48_0_g101, temp_output_49_0_g101 ) * break135_g101.z ) + -0.5 ) * float4( _MainTexDXTScalars , 0.0 ) ) + 0.5 );
				float2 appendResult42_g101 = (float2(_MainTexTinput_TexelSize.z , _MainTexTinput_TexelSize.w));
				float2 temp_output_56_0_g101 = ( temp_output_48_0_g101 * appendResult42_g101 );
				float dotResult58_g101 = dot( temp_output_56_0_g101 , temp_output_56_0_g101 );
				float2 temp_output_54_0_g101 = ( temp_output_49_0_g101 * appendResult42_g101 );
				float dotResult60_g101 = dot( temp_output_54_0_g101 , temp_output_54_0_g101 );
				float temp_output_85_0_g101 = ( max( ( log2( max( dotResult58_g101 , dotResult60_g101 ) ) * 0.5 ) , 0.0 ) / _MainTexInvT_TexelSize.w );
				float2 appendResult88_g101 = (float2(break84_g101.r , temp_output_85_0_g101));
				float2 appendResult86_g101 = (float2(break84_g101.g , temp_output_85_0_g101));
				float2 appendResult87_g101 = (float2(0.0 , temp_output_85_0_g101));
				
				
				float3 Albedo = ( float4( ( _MainTexColorSpaceOrigin + ( _MainTexColorSpaceVector1 * tex2D( _MainTexInvT, appendResult88_g101 ).r ) + ( _MainTexColorSpaceVector2 * tex2D( _MainTexInvT, appendResult86_g101 ).g ) + ( _MainTexColorSpaceVector3 * tex2D( _MainTexInvT, appendResult87_g101 ).b ) ) , 0.0 ) * _Color ).rgb;
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
449;548;1032;461;-4966.755;64.51123;1.567196;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;79;3824,64;Float;True;Property;_MainTexTinput;MainTexTinput;1;0;Create;True;0;0;False;0;22d11f0dd8b20084bb77a147b9c2f0d3;22d11f0dd8b20084bb77a147b9c2f0d3;False;gray;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.Vector3Node;105;3824,448;Float;False;Property;_MainTexColorSpaceOrigin;MainTexColorSpaceOrigin;5;0;Create;True;0;0;False;0;0,0,0;0.2401372,-0.08902974,-0.3312318;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;106;3824,592;Float;False;Property;_MainTexColorSpaceVector1;MainTexColorSpaceVector1;6;0;Create;True;0;0;False;0;0,0,0;1.213327,0.9427675,0.599833;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;109;3824,1024;Float;False;Property;_MainTexDXTScalars;MainTexDXTScalars;9;0;Create;True;0;0;False;0;1,1,1;0.6062531,1.886858,1.481807;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;108;3824,880;Float;False;Property;_MainTexColorSpaceVector3;MainTexColorSpaceVector3;8;0;Create;True;0;0;False;0;0,0,0;-0.117331,-0.24264,0.6186954;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;107;3824,736;Float;False;Property;_MainTexColorSpaceVector2;MainTexColorSpaceVector2;7;0;Create;True;0;0;False;0;0,0,0;-0.3470023,0.3909135,0.08750208;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexturePropertyNode;92;3824,256;Float;True;Property;_MainTexInvT;MainTexInvT;2;1;[NoScaleOffset];Create;True;0;0;False;0;10082244b49df384e9240507fcf2400d;10082244b49df384e9240507fcf2400d;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.FunctionNode;181;4560,704;Inherit;True;StochasticUV;-1;;1;2a9d09a929865a142ba749e628813d62;0;1;53;SAMPLER2D;;False;4;FLOAT2;46;FLOAT2;47;FLOAT2;48;FLOAT4;0
Node;AmplifyShaderEditor.FunctionNode;179;4896,64;Inherit;True;StochasticTextureSampler;-1;;101;444ae51c65aa4be4a80703bd19859863;0;11;104;SAMPLER2D;;False;105;SAMPLER2D;;False;139;FLOAT2;0,0;False;141;FLOAT2;0,0;False;142;FLOAT2;0,0;False;138;FLOAT4;0,0,0,0;False;106;FLOAT3;0,0,0;False;107;FLOAT3;0,0,0;False;108;FLOAT3;0,0,0;False;109;FLOAT3;0,0,0;False;110;FLOAT3;0,0,0;False;1;FLOAT3;103
Node;AmplifyShaderEditor.ColorNode;104;5024,384;Float;False;Property;_Color;Color;0;0;Create;True;0;0;False;0;0,0,0,0;0.4103346,0.5659999,0.3163939,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;5344,64;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;198;5696,1184;Inherit;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;139;3824,2144;Float;False;Property;_NormalDXTScalars;NormalDXTScalars;14;0;Create;True;0;0;False;0;1,1,1;0.762998,0.8818905,2.352584;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;141;3824,1856;Float;False;Property;_NormalColorSpaceVector2;NormalColorSpaceVector2;12;0;Create;True;0;0;False;0;0,0,0;-0.7723546,0.8299038,0.02279877;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;178;4896,1184;Inherit;True;StochasticTextureSampler;-1;;102;444ae51c65aa4be4a80703bd19859863;0;11;104;SAMPLER2D;;False;105;SAMPLER2D;;False;139;FLOAT2;0,0;False;141;FLOAT2;0,0;False;142;FLOAT2;0,0;False;138;FLOAT4;0,0,0,0;False;106;FLOAT3;0,0,0;False;107;FLOAT3;0,0,0;False;108;FLOAT3;0,0,0;False;109;FLOAT3;0,0,0;False;110;FLOAT3;0,0,0;False;1;FLOAT3;103
Node;AmplifyShaderEditor.Vector3Node;142;3824,1568;Float;False;Property;_NormalColorSpaceOrigin;NormalColorSpaceOrigin;10;0;Create;True;0;0;False;0;0,0,0;0.4390354,-0.3666983,0.5481136;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexturePropertyNode;136;3824,1184;Float;True;Property;_NormalTinput;NormalTinput;3;1;[NoScaleOffset];Create;True;0;0;False;0;24fc3483329347042b50e2c668f3fb0b;24fc3483329347042b50e2c668f3fb0b;False;bump;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TexturePropertyNode;137;3824,1376;Float;True;Property;_NormalInvT;NormalInvT;4;1;[NoScaleOffset];Create;True;0;0;False;0;877e255f7f911d042bf2a944b6336d52;877e255f7f911d042bf2a944b6336d52;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;214;5664,256;Inherit;False;Property;_Falloff;Falloff;17;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;140;3824,2000;Float;False;Property;_NormalColorSpaceVector3;NormalColorSpaceVector3;13;0;Create;True;0;0;False;0;0,0,0;-0.01343385,-0.02415477,0.424165;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode;206;5280,1184;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;207;5520,1184;Inherit;False;FLOAT4;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;200;5488,1344;Inherit;False;Property;_NormalScale;Normal Scale;16;0;Create;True;0;0;False;0;4.14;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;128;5664,176;Float;False;Property;_Smoothness;Smoothness;15;0;Create;True;0;0;False;0;0;0.046;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;138;3824,1712;Float;False;Property;_NormalColorSpaceVector1;NormalColorSpaceVector1;11;0;Create;True;0;0;False;0;0,0,0;0.9586893,0.8899831,0.08104445;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;215;5664,336;Inherit;False;Property;_EdgeLight;Edge Light;18;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;212;5984,64;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;208;5984,-281;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;15;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;Outline;0;0;Outline;7;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;1;False;-1;False;False;False;False;False;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;210;5984,64;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;211;5984,64;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;213;5984,64;Float;False;False;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;af8c29dd691e0e14fbfe92fd55725540;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;209;5984,64;Float;False;True;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;15;Stochastic Cel Shaded;af8c29dd691e0e14fbfe92fd55725540;True;Forward;0;1;Forward;13;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;11;Workflow;1;Surface;0;  Blend;0;Two Sided;1;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Vertex Position,InvertActionOnDeselection;1;0;6;True;True;True;True;True;True;False;0
WireConnection;181;53;79;0
WireConnection;179;104;79;0
WireConnection;179;105;92;0
WireConnection;179;139;181;46
WireConnection;179;141;181;47
WireConnection;179;142;181;48
WireConnection;179;138;181;0
WireConnection;179;106;105;0
WireConnection;179;107;106;0
WireConnection;179;108;107;0
WireConnection;179;109;108;0
WireConnection;179;110;109;0
WireConnection;103;0;179;103
WireConnection;103;1;104;0
WireConnection;198;0;207;0
WireConnection;198;1;200;0
WireConnection;178;104;136;0
WireConnection;178;105;137;0
WireConnection;178;139;181;46
WireConnection;178;141;181;47
WireConnection;178;142;181;48
WireConnection;178;138;181;0
WireConnection;178;106;142;0
WireConnection;178;107;138;0
WireConnection;178;108;141;0
WireConnection;178;109;140;0
WireConnection;178;110;139;0
WireConnection;206;0;178;103
WireConnection;207;1;206;1
WireConnection;207;3;206;0
WireConnection;209;0;103;0
WireConnection;209;1;198;0
WireConnection;209;4;128;0
WireConnection;209;11;214;0
WireConnection;209;12;215;0
ASEEND*/
//CHKSM=C06BB5FCD1665845E24E6762CCDB0E27BDD2E8B9