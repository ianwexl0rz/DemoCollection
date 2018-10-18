// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Low Precision (Vertex Lit)" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Precision ("Precision", float) = 240
		//_UseScreenSpace ("Use Screen Space", bool) = true
		_Color ("Color", COLOR) = (1,1,1,1)
		_Fresnel ("Fresnel", COLOR) = (0,0,0,1)
		_SpecColor ("Specular", COLOR) = (0,0,0,1)
		_Shininess ("Shininess", Range(0.0,1.0)) = 0.5
	}
	SubShader {
	    Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
	    Pass {
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma target 3.0
			
			//uniform float4 _LightColor0;
			uniform int _Precision;
			uniform float4 _Color;
			uniform float4 _Fresnel;
			uniform float4 _Emission;
			uniform sampler2D _MainTex;
	        //uniform float4 _SpecColor; 
	        uniform float _Shininess;
	        //uniform float _Range;
	        
			float4 _MainTex_ST;
			
			struct vertexIn
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};
			
			struct vertOut {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 scrPos : TEXCOORD1;
				
				float4 color : COLOR;
			};
			
			inline float4 LowPrecision(float4 pos, float precision)
			{
				float4 lowPos = pos;
				
				//remove perspective, convert (-1,1) to (0,1)
				lowPos = lowPos / pos.w * 0.5 + 0.5;
				
				//calculate new xyz coordinates
				lowPos = floor(lowPos * precision + 0.5) / precision;
				
				//convert (0,1) to (-1,1), restore perspective
				lowPos = (lowPos * 2.0 - 1.0) * pos.w;
				
				return lowPos;
			}
			
			inline float4 LowPrecisionScreen(float4 pos, float precisionY)
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
			
			vertOut vert(vertexIn v) {
				vertOut o;
				o.pos = UnityObjectToClipPos(v.vertex); // use model * view * projection
				o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
				
				float3 viewpos = mul (unity_ObjectToWorld, v.vertex).xyz;
                float3 viewN = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal).xyz;
                float3 lightColor = UNITY_LIGHTMODEL_AMBIENT.xyz * 2.0  * _Color;
                float3 toLight = _WorldSpaceLightPos0.xyz - viewpos.xyz * _WorldSpaceLightPos0.w;
                float lengthSqr = dot(toLight, toLight);
                
				//unity_LightAtten[0].z;
                float atten = 1.0 / (1.0 + lengthSqr * 0);//(25.0 / rangeSqr));
                //float atten = 1.0 / (1.0 + lengthSqr * (25.0 / 100));//(25.0 / rangeSqr));
                //atten = step(1.0 / 26.0, atten) * atten;
                //0.0384614
                
                float3 lightDir = normalize(mul((float3x3)UNITY_MATRIX_V,toLight));
                float3 viewDir = normalize(mul((float3x3)UNITY_MATRIX_V,_WorldSpaceCameraPos - viewpos.xyz));
                half3 h = normalize (lightDir + viewDir);
                
				float diff = max (0, dot (viewN, lightDir));
				float vh = max (0, dot (viewN, viewDir));
				float nh = max (0, dot (viewN, h));
				
				
				float fresnel = pow(1.0 - vh, 4);// * _Fresnel.rgb;
				float spec = pow (nh, _Shininess * 128.0);// * s.Gloss;
				                
                lightColor += _LightColor0.rgb * _Color * diff  * (atten * 2);
                lightColor += _LightColor0.rgb * _SpecColor.rgb * spec * (atten);
                lightColor += fresnel * _Emission.rgb;
                
                o.color = float4(lightColor,1);


				//o.color.rgb = ShadeVertexLights (v.vertex, v.normal);
				//o.color.a;
                
				o.pos = LowPrecisionScreen(o.pos, _Precision);
				
				// use camera space
				//o.pos = mul(UNITY_MATRIX_MV,v.vertex);
				//o.pos = LowPrecision(o.pos,_Precision);
				//o.pos = mul(UNITY_MATRIX_P, o.pos);
				
				// use world space
				//o.pos = mul(_Object2World,v.vertex);
				//o.pos = LowPrecision(o.pos,_Precision);
				//o.pos = mul(UNITY_MATRIX_VP, o.pos);
				
				o.scrPos = ComputeScreenPos(o.pos);
				
				return o;
			}
			
			float4 frag(vertOut i) : COLOR {
			
				float2 wcoord = (i.scrPos.xy/i.scrPos.w);
				float depth = 1 - i.scrPos.z * _ProjectionParams.w;
				
				half4 c = float4(1.0,1.0,1.0,1.0);
                //c.r = 1.0 - wcoord.x;
                //c.g = 1.0 - wcoord.y;
                
                c.rgb *= lerp(half4(1.0,0.0,0.0,1.0), half4(0.0,0.0,1.0,1.0),wcoord.x);
                c.rgb += lerp(half4(0.0,1.0,0.0,1.0), half4(0.0,0.0,0.0,1.0),wcoord.y);
                
                //i.color += c;
                //i.color *= _Color;
                i.color *= tex2D(_MainTex, i.uv);
                //i.color *= pow(depth,100.0);
                
                return i.color;
          }
		ENDCG
		}
	}
}
