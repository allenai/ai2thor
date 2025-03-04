Shader "Hidden/Noise Shader RGB" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_GrainTex ("Base (RGB)", 2D) = "gray" {}
	_ScratchTex ("Base (RGB)", 2D) = "gray" {}
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f { 
	float4 pos	: SV_POSITION;
	float2 uv	: TEXCOORD0;
	float2 uvg	: TEXCOORD1; // grain
	float2 uvs	: TEXCOORD2; // scratch
}; 

uniform sampler2D _MainTex;
uniform sampler2D _GrainTex;
uniform sampler2D _ScratchTex;

uniform float4 _GrainOffsetScale;
uniform float4 _ScratchOffsetScale;
uniform fixed4 _Intensity; // x=grain, y=scratch

half4 _MainTex_ST;

v2f vert (appdata_img v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = UnityStereoScreenSpaceUVAdjust(MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord), _MainTex_ST);
	o.uvg = v.texcoord.xy * _GrainOffsetScale.zw + _GrainOffsetScale.xy;
	o.uvs = v.texcoord.xy * _ScratchOffsetScale.zw + _ScratchOffsetScale.xy;
	return o;
}

fixed4 frag (v2f i) : SV_Target
{
	fixed4 col = tex2D(_MainTex, i.uv);
	
	// sample noise texture and do a signed add
	fixed3 grain = tex2D(_GrainTex, i.uvg).rgb * 2 - 1;
	col.rgb += grain * _Intensity.x;

	// sample scratch texture and do a signed add
	fixed3 scratch = tex2D(_ScratchTex, i.uvs).rgb * 2 - 1;
	col.rgb += scratch * _Intensity.y;

	return col;
}
ENDCG
	}
}

Fallback off

}
