Shader "PQ_TransBlue"
{
	Properties 
	{
_Tex("_Tex", 2D) = "black" {}
_Rim_Val("_Rim_Val", Range(0,5) ) = 1
_Rim_Color("_Rim_Color", Color) = (0.5019608,0.5019608,0.5019608,1)
_Rim_Power("_Rim_Power", Range(-2,2) ) = 0.5
_Tex_Spec("_Tex_Spec", 2D) = "black" {}
_Spec_Area("_Spec_Area", Range(0,1) ) = 0.5
_Fix_Value("_Fix_Value", Float) = 1
_CubeMap("_CubeMap", Cube) = "black" {}
_Reflect_Val("_Reflect_Val", Range(0,1) ) = 0.5

	}
	
	SubShader 
	{
		Tags
		{
"Queue"="Transparent"
"IgnoreProjector"="False"
"RenderType"="Opaque"

		}

		
Cull Back
ZWrite On
ZTest LEqual
ColorMask RGBA
Fog{
}


		CGPROGRAM
#pragma surface surf BlinnPhongEditor  alpha decal:blend vertex:vert
#pragma target 2.0


sampler2D _Tex;
float _Rim_Val;
float4 _Rim_Color;
float _Rim_Power;
sampler2D _Tex_Spec;
float _Spec_Area;
float _Fix_Value;
samplerCUBE _CubeMap;
float _Reflect_Val;

			struct EditorSurfaceOutput {
				half3 Albedo;
				half3 Normal;
				half3 Emission;
				half3 Gloss;
				half Specular;
				half Alpha;
				half4 Custom;
			};
			
			inline half4 LightingBlinnPhongEditor_PrePass (EditorSurfaceOutput s, half4 light)
			{
half3 spec = light.a * s.Gloss;
half4 c;
c.rgb = (s.Albedo * light.rgb + light.rgb * spec);
c.a = s.Alpha;
return c;

			}

			inline half4 LightingBlinnPhongEditor (EditorSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
			{
				half3 h = normalize (lightDir + viewDir);
				
				half diff = max (0, dot ( lightDir, s.Normal ));
				
				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, s.Specular*128.0);
				
				half4 res;
				res.rgb = _LightColor0.rgb * diff;
				res.w = spec * Luminance (_LightColor0.rgb);
				res *= atten * 2.0;

				return LightingBlinnPhongEditor_PrePass( s, res );
			}
			
			struct Input {
				float2 uv_Tex;
float3 viewDir;
float3 simpleWorldRefl;
float2 uv_Tex_Spec;

			};

			void vert (inout appdata_full v) {
float4 VertexOutputMaster0_0_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_1_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_2_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_3_NoInput = float4(0,0,0,0);

//o.simpleWorldRefl = -reflect( normalize(WorldSpaceViewDir(v.vertex)), normalize(mul((float3x3)_Object2World, SCALED_NORMAL)));

			}
			

			void surf (Input IN, inout EditorSurfaceOutput o) {
				o.Normal = float3(0.0,0.0,1.0);
				o.Alpha = 1.0;
				o.Albedo = 0.0;
				o.Emission = 0.0;
				o.Gloss = 0.0;
				o.Specular = 0.0;
				o.Custom = 0.0;
				
float4 Tex2D0=tex2D(_Tex,(IN.uv_Tex.xyxy).xy);
float4 Multiply2=Tex2D0 * _Rim_Color;
float4 Multiply1=Multiply2 * _Rim_Power.xxxx;
float4 Multiply3=float4( IN.viewDir.x, IN.viewDir.y,IN.viewDir.z,1.0 ) * _Fix_Value.xxxx;
float4 Fresnel0_1_NoInput = float4(0,0,1,1);
float4 Fresnel0=(1.0 - dot( normalize( Multiply3.xyz), normalize( Fresnel0_1_NoInput.xyz ) )).xxxx;
float4 Pow0=pow(Fresnel0,_Rim_Val.xxxx);
float4 Multiply0=Multiply1 * Pow0;
float4 TexCUBE0=texCUBE(_CubeMap,float4( IN.simpleWorldRefl.x, IN.simpleWorldRefl.y,IN.simpleWorldRefl.z,1.0 ));
float4 Multiply4=_Reflect_Val.xxxx * TexCUBE0;
float4 Add0=Multiply0 + Multiply4;
float4 Tex2D1=tex2D(_Tex_Spec,(IN.uv_Tex_Spec.xyxy).xy);
float4 Master0_1_NoInput = float4(0,0,1,1);
float4 Master0_7_NoInput = float4(0,0,0,0);
float4 Master0_6_NoInput = float4(1,1,1,1);
o.Albedo = Tex2D0;
o.Emission = Add0;
o.Specular = _Spec_Area.xxxx;
o.Gloss = Tex2D1;
o.Alpha = Tex2D0.aaaa;

				o.Normal = normalize(o.Normal);
			}
		ENDCG
	}
	Fallback "Diffuse"
}