// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/MirrorOld"
{
	Properties
	{
		_DetailTex ("Base (RGB)", 2D) = "white" {}
		[HideInInspector] _MainTex ("", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
 
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 refl : TEXCOORD1;
				float4 pos : SV_POSITION;
			};
			float4 _DetailTex_ST;
			v2f vert(float4 pos : POSITION, float2 uv : TEXCOORD0)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (pos);
				o.uv = TRANSFORM_TEX(uv, _DetailTex);
				o.refl = ComputeScreenPos (o.pos);
				return o;
			}
			sampler2D _MainTex;
			sampler2D _DetailTex;
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_DetailTex, i.uv);
				i.refl.w = max(0.001, i.refl.w);
				fixed4 refl = tex2Dproj(_MainTex, UNITY_PROJ_COORD(i.refl));
				return tex * refl;
			}
			ENDCG
	    }
	}
}