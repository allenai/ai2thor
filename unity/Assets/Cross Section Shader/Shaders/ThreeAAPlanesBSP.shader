Shader "CrossSection/ThreeAAPlanesBSP" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_CrossColor("Cross Section Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Plane1Position("Plane1Position",Vector) = (0,0,0,1)
		_Plane2Position("Plane2Position",Vector) = (0,0,0,1)
		_Plane3Position("Plane3Position",Vector) = (0,0,0,1)
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" }
		//LOD 200
		
		Cull Back
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;

		float3 worldPos;
	};

	half _Glossiness;
	half _Metallic;
	fixed4 _Color;
	fixed4 _CrossColor;
	fixed3 _Plane1Position;
	fixed3 _Plane2Position;
	fixed3 _Plane3Position;
	bool checkVisability(fixed3 worldPos)
	{
		float dotProd1 = dot(worldPos - _Plane1Position, fixed3(1,0,0));
		float dotProd2 = dot(worldPos - _Plane2Position, fixed3(0, 1, 0));
		float dotProd3 = dot(worldPos - _Plane3Position, fixed3(0, 0, 1));
		return dotProd1 > 0 && dotProd2 > 0 && dotProd3 > 0;
	}
	void surf(Input IN, inout SurfaceOutputStandard o) {
		if (checkVisability(IN.worldPos))discard;
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;
		// Metallic and smoothness come from slider variables
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = c.a;
	}
	ENDCG

		Cull Front
		CGPROGRAM
#pragma surface surf NoLighting  noambient

	struct Input {
		half2 uv_MainTex;
		float3 worldPos;

	};
	sampler2D _MainTex;
	fixed4 _Color;
	fixed4 _CrossColor;
	fixed3 _Plane1Position;
	fixed3 _Plane2Position;
	fixed3 _Plane3Position;
	bool checkVisability(fixed3 worldPos)
	{
		float dotProd1 = dot(worldPos - _Plane1Position, fixed3(1, 0, 0));
		float dotProd2 = dot(worldPos - _Plane2Position, fixed3(0, 1, 0));
		float dotProd3 = dot(worldPos - _Plane3Position, fixed3(0, 0, 1));
		return dotProd1 > 0 && dotProd2 > 0 && dotProd3 > 0;
	}
	fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
	{
		fixed4 c;
		c.rgb = s.Albedo;
		c.a = s.Alpha;
		return c;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		if (checkVisability(IN.worldPos))discard;
		o.Albedo = _CrossColor;

	}
	ENDCG

	}
		//FallBack "Diffuse"
}
