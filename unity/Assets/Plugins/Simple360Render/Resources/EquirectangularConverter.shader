// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Credit: https://github.com/Mapiarz/CubemapToEquirectangular/blob/master/Assets/Shaders/CubemapToEquirectangular.shader

Shader "Hidden/I360CubemapToEquirectangular"
{
	Properties
	{
		_MainTex ("Cubemap (RGB)", CUBE) = "" {}
		_PaddingX ("Padding X", Float) = 0.0
	}

	Subshader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				//#pragma fragmentoption ARB_precision_hint_nicest
				#include "UnityCG.cginc"

				#define PI    3.141592653589793
				#define TWOPI 6.283185307179587

				struct v2f
				{
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};
		
				samplerCUBE _MainTex;
				float _PaddingX;
				
				v2f vert(appdata_img v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = (v.texcoord.xy + float2(_PaddingX,0)) * float2(TWOPI, PI);
					return o;
				}
		
				fixed4 frag(v2f i) : COLOR 
				{
					float theta = i.uv.y;
					float phi = i.uv.x;
					float3 unit = float3(0,0,0);

					unit.x = sin(phi) * sin(theta) * -1;
					unit.y = cos(theta) * -1;
					unit.z = cos(phi) * sin(theta) * -1;

					return texCUBE(_MainTex, unit);
				}
			ENDCG
		}
	}
	Fallback Off
}