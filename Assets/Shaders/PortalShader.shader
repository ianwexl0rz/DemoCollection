// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Amplify/Portal"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_PortalColor("PortalColor", Color) = (0.003838672,0.5220588,0.243292,0)
		_Distortion("Distortion", Range( 0 , 1)) = 0.292
		_BrushedMetalNormal("BrushedMetalNormal", 2D) = "bump" {}

	}
	
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		LOD 100
		Cull Back

		GrabPass{ }

		Pass
		{
			CGPROGRAM
			#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
			#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
			#else
			#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
			#endif

			#pragma target 3.0 
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord1 : TEXCOORD1;
			};

			uniform sampler2D _MainTex;
			uniform fixed4 _Color;
			ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
			uniform sampler2D _BrushedMetalNormal;
			uniform float4 _BrushedMetalNormal_ST;
			uniform float _Distortion;
			uniform float4 _PortalColor;
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.texcoord.xy = v.texcoord.xy;
				o.texcoord.zw = v.texcoord1.xy;
				
				// ase common template code
				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord1 = screenPos;
				
				
				v.vertex.xyz +=  float3(0,0,0) ;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				fixed4 myColorVar;
				// ase common template code
				float4 screenPos = i.ase_texcoord1;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 normalizedClip21 = ase_grabScreenPosNorm;
				float2 uv0_BrushedMetalNormal = i.texcoord.xy * _BrushedMetalNormal_ST.xy + _BrushedMetalNormal_ST.zw;
				float cos33 = cos( _Time.y );
				float sin33 = sin( _Time.y );
				float2 rotator33 = mul( uv0_BrushedMetalNormal - float2( 0.5,0.5 ) , float2x2( cos33 , -sin33 , sin33 , cos33 )) + float2( 0.5,0.5 );
				float4 screenColor8 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( normalizedClip21 + float4( ( UnpackNormal( tex2D( _BrushedMetalNormal, rotator33 ) ) * _Distortion ) , 0.0 ) ).xy/( normalizedClip21 + float4( ( UnpackNormal( tex2D( _BrushedMetalNormal, rotator33 ) ) * _Distortion ) , 0.0 ) ).w);
				
				
				myColorVar = ( screenColor8 * _PortalColor );
				return myColorVar;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=17200
486;909;1601;442;988.4099;70.29385;1;True;False
Node;AmplifyShaderEditor.Vector2Node;35;-1152,304;Float;False;Constant;_Vector0;Vector 0;-1;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TimeNode;36;-1153.522,435.0121;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;34;-1152,176;Inherit;False;0;29;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;33;-831.5794,259.398;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GrabScreenPosition;39;-521.3355,-27.96944;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;31;-544,400;Float;False;Property;_Distortion;Distortion;1;0;Create;True;0;0;False;0;0.292;0.019;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;29;-542.58,195.399;Inherit;True;Property;_BrushedMetalNormal;BrushedMetalNormal;2;0;Create;True;0;0;False;0;-1;None;302951faffe230848aa0d3df7bb70faa;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;21;-256,-16;Float;False;normalizedClip;-1;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-128.7738,261.0988;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;30;36.62508,137.2995;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;37;231.2177,270.9001;Float;False;Property;_PortalColor;PortalColor;0;0;Create;True;0;0;False;0;0.003838672,0.5220588,0.243292,0;1,0.794,0.9469394,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenColorNode;8;224.0004,85.8997;Float;False;Global;_ScreenGrab0;Screen Grab 0;-1;0;Create;True;0;0;False;0;Object;-1;False;True;1;0;FLOAT4;0,0,0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;504.2176,117.4984;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;60;704.4994,-14.23482;Float;False;True;2;ASEMaterialInspector;0;4;Amplify/Portal;6e114a916ca3e4b4bb51972669d463bf;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;False;False;False;True;0;False;-1;False;False;False;False;False;True;1;RenderType=Transparent=RenderType;False;0;False;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;0;1;True;False;0
WireConnection;33;0;34;0
WireConnection;33;1;35;0
WireConnection;33;2;36;2
WireConnection;29;1;33;0
WireConnection;21;0;39;0
WireConnection;32;0;29;0
WireConnection;32;1;31;0
WireConnection;30;0;21;0
WireConnection;30;1;32;0
WireConnection;8;0;30;0
WireConnection;38;0;8;0
WireConnection;38;1;37;0
WireConnection;60;0;38;0
ASEEND*/
//CHKSM=001AFB46001B609B7C6EE52B101D1F3103591EC6