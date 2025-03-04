// Final pass in the contrast stretch effect: apply
// color stretch to the original image, based on currently
// adapted to minimum/maximum luminances.

Shader "Hidden/Contrast Stretch Apply" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_AdaptTex ("Base (RGB)", 2D) = "white" {}
}

Category {
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
				
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f {
	float4 pos		: SV_POSITION;
	float2 uv[2]	: TEXCOORD0;
}; 

uniform sampler2D _MainTex;
uniform sampler2D _AdaptTex;
half4 _MainTex_ST;

v2f vert (appdata_img v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv[0] = MultiplyUV (UNITY_MATRIX_TEXTURE0, UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST));
	o.uv[1] = float2(0.5,0.5);
	return o;
}

float4 frag (v2f i) : SV_Target
{
	float4 col = tex2D(_MainTex, i.uv[0]);
	float4 adapted = tex2D(_AdaptTex, i.uv[1]);
	float vmul = 1.0 / adapted.z;
	float vadd = -adapted.w;
	col.rgb = col.rgb * vmul + vadd;	
	return col;
}
ENDCG

		}
	}
}

Fallback off

}
