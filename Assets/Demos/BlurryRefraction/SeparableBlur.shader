// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/SeparableGlassBlur" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "gray" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;

		float4 uv01 : TEXCOORD1;
		float4 uv23 : TEXCOORD2;
		float4 uv45 : TEXCOORD3;
	};
	
	float blur;
	float4 offsets;
	
	sampler2D _MainTex;
	
	v2f vert (appdata_img v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		o.uv.xy = v.texcoord.xy;

		o.uv01 =  v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1);
		o.uv23 =  v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1) * 2.0;
		o.uv45 =  v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1) * 3.0;

		return o;
	}
	
	half4 frag (v2f i) : COLOR {
		half4 color = float4 (0,0,0,0);

		color += tex2D (_MainTex, i.uv) * (1.0 - blur);
		color += tex2D (_MainTex, i.uv01.xy) * blur / 4.0;
		color += tex2D (_MainTex, i.uv01.zw) * blur / 4.0;
		color += tex2D (_MainTex, i.uv23.xy) * blur / 6.0;
		color += tex2D (_MainTex, i.uv23.zw) * blur / 6.0;
		color += tex2D (_MainTex, i.uv45.xy) * blur / 12.0;
		color += tex2D (_MainTex, i.uv45.zw) * blur / 12.0;
		
		return color;
	}

	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }

      CGPROGRAM
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off


} // shader
