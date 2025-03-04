Shader "Hidden/LensFlareCreate" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv[4] : TEXCOORD0;
	};
		
	fixed4 colorA;
	fixed4 colorB; 
	fixed4 colorC; 
	fixed4 colorD; 
	
	sampler2D _MainTex;
	half4     _MainTex_ST;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		o.uv[0] = UnityStereoScreenSpaceUVAdjust(( ( v.texcoord.xy - 0.5 ) * -0.85 ) + 0.5, _MainTex_ST);
		o.uv[1] = UnityStereoScreenSpaceUVAdjust(( ( v.texcoord.xy - 0.5 ) * -1.45 ) + 0.5, _MainTex_ST);
		o.uv[2] = UnityStereoScreenSpaceUVAdjust(( ( v.texcoord.xy - 0.5 ) * -2.55 ) + 0.5, _MainTex_ST);
		o.uv[3] = UnityStereoScreenSpaceUVAdjust(( ( v.texcoord.xy - 0.5 ) * -4.15 ) + 0.5, _MainTex_ST);
		return o;
	}
	
	fixed4 frag(v2f i) : SV_Target {
		fixed4 color = float4 (0,0,0,0);
		color += tex2D(_MainTex, i.uv[0] ) * colorA;
		color += tex2D(_MainTex, i.uv[1] ) * colorB;
		color += tex2D(_MainTex, i.uv[2] ) * colorC;
		color += tex2D(_MainTex, i.uv[3] ) * colorD;
		return color;
	}

	ENDCG
	
Subshader {
 Blend One One
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
