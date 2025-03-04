Shader "Hidden/SeparableBlur" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;

		float4 uv01 : TEXCOORD1;
		float4 uv23 : TEXCOORD2;
		float4 uv45 : TEXCOORD3;
	};
	
	float4 offsets;
	
	sampler2D _MainTex;
	half4 _MainTex_ST;
		
	v2f vert (appdata_img v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		o.uv.xy = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);

		o.uv01 = UnityStereoScreenSpaceUVAdjust(v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1), _MainTex_ST);
		o.uv23 = UnityStereoScreenSpaceUVAdjust(v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1) * 2.0, _MainTex_ST);
		o.uv45 = UnityStereoScreenSpaceUVAdjust(v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1) * 3.0, _MainTex_ST);

		return o;  
	}
		
	half4 frag (v2f i) : SV_Target {
		half4 color = float4 (0,0,0,0);

		color += 0.40 * tex2D (_MainTex, i.uv);
		color += 0.15 * tex2D (_MainTex, i.uv01.xy);
		color += 0.15 * tex2D (_MainTex, i.uv01.zw);
		color += 0.10 * tex2D (_MainTex, i.uv23.xy);
		color += 0.10 * tex2D (_MainTex, i.uv23.zw);
		color += 0.05 * tex2D (_MainTex, i.uv45.xy);
		color += 0.05 * tex2D (_MainTex, i.uv45.zw);
		
		return color;
	} 

	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off

	
} // shader
