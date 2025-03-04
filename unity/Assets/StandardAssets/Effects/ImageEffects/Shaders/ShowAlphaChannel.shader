

Shader "Hidden/ShowAlphaChannel" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_EdgeTex ("_EdgeTex", 2D) = "white" {}
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off

CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

uniform sampler2D _MainTex;
uniform sampler2D _EdgeTex;

uniform float4 _MainTex_TexelSize;

float filterRadius;

struct v2f {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = v.texcoord.xy;
	
	return o;
}

half4 frag (v2f i) : SV_Target
{

	half4 color = tex2D(_MainTex,  i.uv.xy);
	half edges = color.a;
	
	return edges;
}
ENDCG
	}
}

Fallback off

}
