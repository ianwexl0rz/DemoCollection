Shader "Cel Shaded Template"
{
	Properties
	{
		_MainColor("Main Color", Color) = (1,1,1,0)
		_SecondaryColor("Secondary Color", Color) = (0,0,0,0)
		_Translucency("Translucency", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0
		_EdgeLight("Edge Light", Range(0, 1)) = 0
		_Float0("Float 0", Float) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "CustomPBSLighting.cginc"
		#pragma target 3.0
		#pragma surface surf Custom keepalpha addshadow fullforwardshadows exclude_path:forward vertex:vertexDataFunc 
		struct Input
		{
			float4 screenPos;
		};

		uniform float _Float0;
		uniform float4 _MainColor;
		uniform float4 _SecondaryColor;
		uniform half _Translucency;
		uniform half _Smoothness;
		uniform half _EdgeLight;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( ase_vertexNormal * _Float0 );
		}

		void surf( Input i , inout SurfaceOutputCustom o )
		{
			o.ScreenPos = (i.screenPos.xy / i.screenPos.w) * _ScreenParams.xy;
			o.Albedo = _MainColor.rgb;
			o.SecondaryColor = _SecondaryColor;
			o.Translucency = _Translucency;
			o.Smoothness = _Smoothness;
			o.EdgeLight = _EdgeLight;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}