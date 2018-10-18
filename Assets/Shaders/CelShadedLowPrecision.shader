// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Low Precision (Cel Shaded)"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,0)
		_SecondaryColor("Secondary Color", Color) = (0,0,0,0)
		_Translucency("Translucency", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0
		_EdgeLight("Edge Light", Range(0, 1)) = 0
		[HDR] _EmissionColor("Emission", Color) = (0,0,0,0)
		_Precision("Precision", float) = 240
		[HideInInspector] _texcoord("", 2D) = "white" {}
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
			float2 uv_texcoord;
		};

		uniform sampler2D _MainTex;
		uniform float4 _Color;
		uniform float4 _SecondaryColor;
		uniform half _Translucency;
		uniform half _Smoothness;
		uniform half _EdgeLight;
		uniform half3 _EmissionColor;
		uniform float _Precision;

		uniform float4 _MainTex_ST;


		float3 LowPrecision(float3 pos, float precision)
		{
			//calculate new xyz coordinates
			return floor(pos * precision + 0.5) / precision;
		}

		float4 LowPrecisionScreen(float4 pos, float precisionY)
		{
			float4 lowPos = pos;
			float2 precisionX = precisionY * _ScreenParams.x / _ScreenParams.y;

			//remove perspective, convert (-1,1) to (0,1)
			lowPos = lowPos / pos.w * 0.5 + 0.5;

			//calculate new xy coordinates (keep z)
			lowPos.x = floor(lowPos.x * precisionX + 0.5) / precisionX;
			lowPos.y = floor(lowPos.y * precisionY + 0.5) / precisionY;

			//convert (0,1) to (-1,1), restore perspective
			lowPos = (lowPos * 2.0 - 1.0) * pos.w;

			return lowPos;
		}

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			// use camera space
			float3 pos = UnityObjectToViewPos(v.vertex.xyz);
			pos = LowPrecision(pos, _Precision);
			pos = mul(unity_WorldToObject, mul(UNITY_MATRIX_I_V, float4(pos, 1))).xyz;
			v.vertex.xyz = pos;

			// use world space
			//float4 pos = float4(v.vertex.xyz, 0.0);
			//pos = mul(unity_ObjectToWorld, pos);
			//pos = LowPrecision(pos, _Precision);
			//pos = mul(unity_WorldToObject, pos);
			//v.vertex.xyz = pos;

			// use object space
			//v.vertex.xyz = LowPrecision(v.vertex.xyz, _Precision);
			//v.normal.xyz *= -1;

		}

		void surf( Input i , inout SurfaceOutputCustom o )
		{
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			o.Albedo = _Color.rgb * tex2D(_MainTex, uv_MainTex);
			o.ScreenPos = (i.screenPos.xy / i.screenPos.w) * _ScreenParams.xy;
			o.SecondaryColor = _SecondaryColor;
			o.Translucency = _Translucency;
			o.Smoothness = _Smoothness;
			o.EdgeLight = _EdgeLight;
			o.Emission = _EmissionColor;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}