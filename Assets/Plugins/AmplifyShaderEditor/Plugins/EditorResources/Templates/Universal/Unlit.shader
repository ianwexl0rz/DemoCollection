Shader /*ase_name*/ "Hidden/Universal/Unlit" /*end*/
{
	Properties
	{
		/*ase_props*/
	}

	SubShader
	{
		/*ase_subshader_options:Name=Additional Options
			Option:Surface:Opaque,Transparent:Opaque
				Opaque:SetPropertyOnSubShader:RenderType,Opaque
				Opaque:SetPropertyOnSubShader:RenderQueue,Geometry
				Opaque:SetPropertyOnPass:Forward:ZWrite,On
				Opaque:HideOption:  Blend
				Transparent:SetPropertyOnSubShader:RenderType,Transparent
				Transparent:SetPropertyOnSubShader:RenderQueue,Transparent
				Transparent:SetPropertyOnPass:Forward:ZWrite,Off
				Transparent:ShowOption:  Blend
			Option:  Blend:Alpha,Premultiply,Additive,Multiply:Alpha
				Alpha:SetPropertyOnPass:Forward:BlendRGB,SrcAlpha,OneMinusSrcAlpha
				Premultiply:SetPropertyOnPass:Forward:BlendRGB,One,OneMinusSrcAlpha
				Additive:SetPropertyOnPass:Forward:BlendRGB,One,One
				Multiply:SetPropertyOnPass:Forward:BlendRGB,DstColor,Zero
				Alpha,Premultiply,Additive:SetPropertyOnPass:Forward:BlendAlpha,One,OneMinusSrcAlpha
				Multiply:SetPropertyOnPass:Forward:BlendAlpha,One,Zero
				Premultiply:SetDefine:_ALPHAPREMULTIPLY_ON 1
				Alpha,Additive,Multiply,disable:RemoveDefine:_ALPHAPREMULTIPLY_ON 1
				disable:SetPropertyOnPass:Forward:BlendRGB,One,Zero
				disable:SetPropertyOnPass:Forward:BlendAlpha,One,Zero
			Option:Two Sided:On,Cull Back,Cull Front:Cull Back
				On:SetPropertyOnSubShader:CullMode,Off
				Cull Back:SetPropertyOnSubShader:CullMode,Back
				Cull Front:SetPropertyOnSubShader:CullMode,Front
			Option:Cast Shadows:false,true:true
				true:IncludePass:ShadowCaster
				false,disable:ExcludePass:ShadowCaster
			Option:Receive Shadows:false,true:true
				true:RemoveDefine:_RECEIVE_SHADOWS_OFF 1
				false:SetDefine:_RECEIVE_SHADOWS_OFF 1
			Option:GPU Instancing:false,true:true
				true:SetDefine:pragma multi_compile_instancing
				false:RemoveDefine:pragma multi_compile_instancing
			Option:LOD CrossFade:false,true:false
				true:SetDefine:pragma multi_compile _ LOD_FADE_CROSSFADE
				false:RemoveDefine:pragma multi_compile _ LOD_FADE_CROSSFADE
			Option:Built-in Fog:false,true:false
				true:SetDefine:pragma multi_compile_fog
				false:RemoveDefine:pragma multi_compile_fog
				true:SetDefine:ASE_FOG 1
				false:RemoveDefine:ASE_FOG 1
			Option:Meta Pass:false,true:false
				true:IncludePass:Meta
				true:ShowPort:Forward:Baked Albedo
				true:ShowPort:Forward:Baked Emission
				false,disable:ExcludePass:Meta
				false:HidePort:Forward:Baked Albedo
				false:HidePort:Forward:Baked Emission
			Option:Vertex Position,InvertActionOnDeselection:Absolute,Relative:Relative
				Absolute:SetDefine:ASE_ABSOLUTE_VERTEX_POS 1
				Absolute:SetPortName:Forward:5,Vertex Position
				Relative:SetPortName:Forward:5,Vertex Offset
		*/
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType"="Opaque"
			"Queue"="Geometry+0" 
		}
		
		Cull Back
		HLSLINCLUDE
		#pragma target 2.0
		ENDHLSL

		/*ase_pass*/
		Pass
		{
			Name "Forward"
			Tags{"LightMode" = "UniversalForward"}
			
			Blend One Zero
			ZWrite On
			ZTest LEqual
			Offset 0,0
			ColorMask RGBA
			/*ase_stencil*/

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			/*ase_pragma*/

			/*ase_globals*/

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				/*ase_vdata:p=p;n=n;t=t;uv1=tc1.xyzw*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD0;
				#endif
				/*ase_interp(1,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/*ase_funcs*/

			VertexOutput vert ( VertexInput v /*ase_vert_input*/ )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;5;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = /*ase_vert_out:Vertex Normal;Float3;6;-1;_Normal*/v.ase_normal/*end*/;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
				o.clipPos = vertexInput.positionCS;
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( vertexInput.positionCS.z );
				#endif
				return o;
			}

			half4 frag ( VertexOutput IN /*ase_frag_input*/ ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				/*ase_frag_code:IN=VertexOutput*/
				float3 BakedAlbedo = /*ase_frag_out:Baked Albedo;Float3;0;-1;_Albedo*/0/*end*/;
				float3 BakedEmission = /*ase_frag_out:Baked Emission;Float3;1;-1;_Emission*/0/*end*/;
				float3 Color = /*ase_frag_out:Color;Float3;2;-1;_Color*/float3( 0.5, 0.5, 0.5 )/*end*/;
				float Alpha = /*ase_frag_out:Alpha;Float;3;-1;_Alpha*/1/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;4;-1;_AlphaClip*/0.5/*end*/;

				#if _AlphaClip
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}

		/*ase_pass*/
		Pass
		{
			/*ase_hide_pass*/
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			/*ase_pragma*/

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				/*ase_vdata:p=p;n=n*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			/*ase_globals*/

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				/*ase_interp(7,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/*ase_funcs*/

			float3 _LightDirection;

			VertexOutput ShadowPassVertex( VertexInput v/*ase_vert_input*/ )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;2;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = /*ase_vert_out:Vertex Normal;Float3;3;-1;_Normal*/v.ase_normal/*end*/;

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

			half4 ShadowPassFragment(VertexOutput IN /*ase_frag_input*/ ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				/*ase_frag_code:IN=VertexOutput*/
				float Alpha = /*ase_frag_out:Alpha;Float;0;-1;_Alpha*/1/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;1;-1;_AlphaClip*/0.5/*end*/;

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

		/*ase_pass*/
		Pass
		{
			/*ase_hide_pass*/
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			/*ase_pragma*/

			/*ase_globals*/

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				/*ase_vdata:p=p;n=n*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				/*ase_interp(0,):sp=sp*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/*ase_funcs*/

			VertexOutput vert( VertexInput v /*ase_vert_input*/ )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;2;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = /*ase_vert_out:Vertex Normal;Float3;3;-1;_Normal*/v.ase_normal/*end*/;

				o.clipPos = TransformObjectToHClip(v.vertex.xyz);
				return o;
			}

			half4 frag(VertexOutput IN /*ase_frag_input*/ ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				/*ase_frag_code:IN=VertexOutput*/
				float Alpha = /*ase_frag_out:Alpha;Float;0;-1;_Alpha*/1/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;1;-1;_AlphaClip*/0.5/*end*/;

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

		/*ase_pass*/
		Pass
		{
			/*ase_hide_pass*/
			Name "Meta"
			Tags{"LightMode" = "Meta"}

			Cull Off

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			/*ase_pragma*/

			/*ase_globals*/

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				/*ase_vdata:p=p;n=n;uv1=tc1;uv2=tc2*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				/*ase_interp(0,):sp=sp;uv1=tc1*/
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/*ase_funcs*/

			VertexOutput vert( VertexInput v /*ase_vert_input*/ )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				/*ase_vert_code:v=VertexInput;o=VertexOutput*/
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = /*ase_vert_out:Vertex Offset;Float3;4;-1;_Vertex*/defaultVertexValue/*end*/;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = /*ase_vert_out:Vertex Normal;Float3;5;-1;_Normal*/v.ase_normal/*end*/;

				o.clipPos = MetaVertexPosition( v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
				return o;
			}

			half4 frag(VertexOutput IN /*ase_frag_input*/ ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				/*ase_frag_code:IN=VertexOutput*/
				float3 BakedAlbedo = /*ase_frag_out:Baked Albedo;Float3;0;-1;_Albedo*/0/*end*/;
				float3 BakedEmission = /*ase_frag_out:Baked Emission;Float3;1;-1;_Emission*/0/*end*/;
				float Alpha = /*ase_frag_out:Alpha;Float;2;-1;_Alpha*/1/*end*/;
				float AlphaClipThreshold = /*ase_frag_out:Alpha Clip Threshold;Float;3;-1;_AlphaClip*/0.5/*end*/;

				#if _AlphaClip
					clip(Alpha - AlphaClipThreshold);
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = BakedAlbedo;
				metaInput.Emission = BakedEmission;
				
				return MetaFragment(metaInput);
			}
			ENDHLSL
		}
		/*ase_pass_end*/
	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	FallBack "Hidden/InternalErrorShader"
}
