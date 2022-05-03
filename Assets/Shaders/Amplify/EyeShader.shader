// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Universal Render Pipeline/Eye Shader"
{
	Properties
	{
		_EyeColor("Eye Color", Color) = (1,0,0,0)
		_NormalIntensity("Normal Intensity", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _NormalIntensity;
		uniform float4 _EyeColor;


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1);
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1);
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_output_182_0 = ((float2( -1,-1 ) + ((float2( 0,0 ) + (i.uv_texcoord - float2( 0.38,0.28 )) * (float2( 1,1 ) - float2( 0,0 )) / (float2( 0.62,0.72 ) - float2( 0.38,0.28 ))) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 )))).xy;
			float dotResult183 = dot( temp_output_182_0 , temp_output_182_0 );
			float temp_output_70_0 = sqrt( ( 1.0 - saturate( dotResult183 ) ) );
			float3 appendResult222 = (float3(( temp_output_182_0 * _NormalIntensity ) , temp_output_70_0));
			float smoothstepResult228 = smoothstep( 0.8 , 1.0 , dotResult183);
			float3 lerpResult225 = lerp( (float3( 0,0,0 ) + (appendResult222 - float3( -1,-1,-1 )) * (float3( 1,1,1 ) - float3( 0,0,0 )) / (float3( 1,1,1 ) - float3( -1,-1,-1 ))) , float3( 0.5,0.5,1 ) , smoothstepResult228);
			o.Normal = lerpResult225;
			Gradient gradient12 = NewGradient( 0, 5, 2, float4( 0, 0, 0, 0.1117723 ), float4( 0.5, 0.5, 0.5, 0.1294118 ), float4( 0.489, 0.489, 0.489, 0.5147021 ), float4( 0.177, 0.177, 0.177, 0.6588235 ), float4( 1, 1, 1, 0.7353017 ), 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float4 blendOpSrc21 = _EyeColor;
			float4 blendOpDest21 = SampleGradient( gradient12, ( 1.0 - temp_output_70_0 ) );
			o.Albedo = ( saturate( (( blendOpDest21 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest21 ) * ( 1.0 - blendOpSrc21 ) ) : ( 2.0 * blendOpDest21 * blendOpSrc21 ) ) )).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17200
486;1055;1601;296;1771.294;513.0366;1;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;6;-4698.302,-539.9158;Inherit;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;90;-4406.965,-419.0122;Inherit;True;5;0;FLOAT2;0,0;False;1;FLOAT2;0.38,0.28;False;2;FLOAT2;0.62,0.72;False;3;FLOAT2;0,0;False;4;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;71;-4063.489,-418.9032;Inherit;False;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-1,-1;False;4;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;182;-3806.437,-395.534;Inherit;False;True;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;183;-3470.533,106.5071;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;146;-3161.124,-152.2097;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;73;-2872.656,-225.825;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-2723.236,-623.3261;Inherit;False;Property;_NormalIntensity;Normal Intensity;3;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SqrtOpNode;70;-2658.771,-227.752;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;229;-2447.337,-544.5134;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;145;-1205.382,-285.652;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;12;-1060.904,-407.9236;Inherit;False;0;5;2;0,0,0,0.1117723;0.5,0.5,0.5,0.1294118;0.489,0.489,0.489,0.5147021;0.177,0.177,0.177,0.6588235;1,1,1,0.7353017;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.DynamicAppendNode;222;-2143.877,-553.8547;Inherit;True;FLOAT3;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCRemapNode;223;-1807.622,-545.95;Inherit;False;5;0;FLOAT3;0,0,0;False;1;FLOAT3;-1,-1,-1;False;2;FLOAT3;1,1,1;False;3;FLOAT3;0,0,0;False;4;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;20;-1096.978,-694.3401;Inherit;False;Property;_EyeColor;Eye Color;2;0;Create;True;0;0;False;0;1,0,0,0;0.865,0.6004637,0.0237451,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientSampleNode;11;-759.3127,-394.8774;Inherit;True;2;0;OBJECT;0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;228;-1960.034,32.4205;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.8;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;225;-1496.232,-40.16672;Inherit;True;3;0;FLOAT3;0.5,0.5,1;False;1;FLOAT3;0.5,0.5,1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-593.1132,497.1738;Inherit;False;Property;_Smoothness;Smoothness;1;0;Create;True;0;0;False;0;0.4;0.4;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-617.851,633.6169;Inherit;False;Property;_Falloff;Falloff;0;0;Create;True;0;0;False;0;0.4;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;21;-344.5842,-390.2563;Inherit;True;Overlay;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;231;54.69152,-260.335;Float;False;True;2;ASEMaterialInspector;0;0;Standard;Universal Render Pipeline/Eye Shader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;90;0;6;0
WireConnection;71;0;90;0
WireConnection;182;0;71;0
WireConnection;183;0;182;0
WireConnection;183;1;182;0
WireConnection;146;0;183;0
WireConnection;73;0;146;0
WireConnection;70;0;73;0
WireConnection;229;0;182;0
WireConnection;229;1;129;0
WireConnection;145;0;70;0
WireConnection;222;0;229;0
WireConnection;222;2;70;0
WireConnection;223;0;222;0
WireConnection;11;0;12;0
WireConnection;11;1;145;0
WireConnection;228;0;183;0
WireConnection;225;0;223;0
WireConnection;225;2;228;0
WireConnection;21;0;20;0
WireConnection;21;1;11;0
WireConnection;231;0;21;0
WireConnection;231;1;225;0
ASEEND*/
//CHKSM=6ED867B74A117E57BFEB33F0551553E1E859009B