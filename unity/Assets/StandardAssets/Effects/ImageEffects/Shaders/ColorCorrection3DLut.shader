Shader "Hidden/ColorCorrection3DLut" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}		
		_ClutTex ("-", 3D) = "" {}
	}

CGINCLUDE

#include "UnityCG.cginc"

struct v2f {
	float4 pos : SV_POSITION;
	float2 uv  : TEXCOORD0;
};

sampler2D _MainTex;
sampler3D _ClutTex;

half4 _MainTex_ST;

float _Scale;
float _Offset;

v2f vert( appdata_img v ) 
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
	return o;
} 

float4 frag(v2f i) : SV_Target 
{
	float4 c = tex2D(_MainTex, i.uv);
	c.rgb = tex3D(_ClutTex, c.rgb * _Scale + _Offset).rgb;
	return c;
}

float4 fragLinear(v2f i) : SV_Target 
{ 
	float4 c = tex2D(_MainTex, i.uv);
	c.rgb= sqrt(c.rgb);
	c.rgb = tex3D(_ClutTex, c.rgb * _Scale + _Offset).rgb;
	c.rgb = c.rgb*c.rgb; 
	return c;
}

ENDCG 

	
Subshader 
{
	Pass 
	{
	  ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
	  #pragma target 3.0
      ENDCG
  	}

	Pass 
	{
	  ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment fragLinear
	  #pragma target 3.0
      ENDCG
  	}
}

Fallback off
}
