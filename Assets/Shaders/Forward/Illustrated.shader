Shader "Illustrated" {

	Properties {
		[HDR]
	    _Color ("Main Color", Color) = (1,1,1,1)
		[HDR]
	    _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
		[HDR]
	    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0, 1)) = 0.09
	    _MainTex ("Base (RGB)", 2D) = "white" {}
	    _BumpMap ("Bumpmap", 2D) = "bump" {}
	}

	SubShader {
	    Tags { "RenderType" = "Opaque" }
		
		CGPROGRAM
		#pragma surface surf Ramp

		//#pragma target 3.0
		
				
		float4 _Color;
		float4 _ShadowColor;
		half _Shininess;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		
		struct Input {
		    float2 uv_MainTex;
		    float2 uv_BumpMap;
		};

		
		
		void surf (Input IN, inout SurfaceOutput o) {
		
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			//o.Albedo = .5;
			o.Albedo = tex.rgb * _Color.rgb;
			o.Gloss = tex.a;
			o.Alpha = tex.a * _Color.a;
			o.Specular = _Shininess;
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
		}
		
		half4 LightingRamp (SurfaceOutput s, half3 lightDir,  half3 viewDir, half atten) {
		    
			s.Normal = normalize(s.Normal);

		    half3 halfDir = Unity_SafeNormalize(lightDir + viewDir);
	 		
	 		half nl = dot(s.Normal, lightDir);
	 		half nv = DotClamped(s.Normal, viewDir);
			half nh = DotClamped(s.Normal, halfDir);
			half lh = DotClamped(lightDir, halfDir);

			float diff = smoothstep(-.2, 0.1, nl) * 0.3;
			diff += smoothstep(-.3, 1, nl) * 0.3;
			diff += smoothstep(0.3, .6, nl) * 0.4;

			float spec = pow(nh, s.Specular * 256.0 * s.Gloss);

			float fresnel = smoothstep(0.6, 0.4, nv);
			float edgelight = smoothstep(0, 0.8,saturate(nl - nv)) * fresnel;

			//fixed3 shading = _ShadowColor + max(diff,edgelight) * _LightColor0.rgb * atten;
			//fixed3 shading = diff * atten * _LightColor0.rgb;

			fixed4 c;
			c.rgb = s.Albedo * (_ShadowColor  + (1 - _ShadowColor) * diff * atten) * _LightColor0.rgb // shading// - (1 - shading) // (_ShadowColor * (1 - shading))
					+  (max(edgelight, spec) * _LightColor0.rgb * _SpecColor
					+ lerp(s.Albedo.rgb, 1, 0.1) * _LightColor0.rgb * edgelight * 0.5
					) * atten;
			c.a = s.Alpha + _LightColor0.a * _SpecColor.a * spec * atten;
			return c;
		}
		
		ENDCG
		
	}
	
	Fallback " Glossy", 0
}