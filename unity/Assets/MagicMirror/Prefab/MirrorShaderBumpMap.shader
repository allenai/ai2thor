Shader "Custom/MirrorBumpMap"
{
	Properties
	{
 		_MainTex ("Emissive Texture", 2D) = "black" {}
		_DetailTex ("Detail Texture", 2D) = "white" {}
		_BumpMap ("Bump Map Texture", 2D) = "bumpmap" {}
		_Color ("Detail Tint Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		_SpecularArea ("Specular Area", Range (0, 0.99)) = 0.1
		_SpecularIntensity ("Specular Intensity", Range (0, 1)) = 0.75
		_ReflectionColor ("Reflection Tint Color", Color) = (1,1,1,1)
	}
	SubShader
	{ 
		Tags { "RenderType"="Opaque"}
		LOD 300
     
		CGPROGRAM

		#pragma surface surf BlinnPhong
		#pragma multi_compile __ MIRROR_RECURSION
		#include "UnityCG.cginc"
  
		fixed4 _Color;
		fixed4 _ReflectionColor;
		half _SpecularArea;
		half _SpecularIntensity;
		sampler2D _MainTex;
		sampler2D _DetailTex;
		sampler2D _BumpMap;
  
		struct Input
		{
			float2 uv_DetailTex;
			float2 uv_BumpMap;
			float4 screenPos;
		};
 
		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 detail = tex2D(_DetailTex, IN.uv_DetailTex);

			#if MIRROR_RECURSION

			fixed4 refl = tex2D(_MainTex, IN.uv_DetailTex);

			#else

			IN.screenPos.w = max(0.001, IN.screenPos.w);
			fixed4 refl = tex2Dproj(_MainTex, UNITY_PROJ_COORD(IN.screenPos));

			#endif

			o.Albedo = detail.rgb * _Color.rgb;
			o.Alpha = 1;
			o.Specular = 1.0f - _SpecularArea;
			o.Gloss = _SpecularIntensity;
			o.Emission = refl.rgb * _ReflectionColor.rgb;
			o.Normal = UnpackNormal(tex2D (_BumpMap, IN.uv_BumpMap));
		}

		ENDCG
	}
 
	FallBack "Reflective/Specular"
}