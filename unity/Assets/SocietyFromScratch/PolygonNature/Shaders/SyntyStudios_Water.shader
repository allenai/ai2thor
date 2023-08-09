// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/Water"
{
	Properties
	{
		_WaterNormal("Water Normal", 2D) = "bump" {}
		_WaterScale("Water Scale", Range( 0 , 1)) = 0
		_WaterSpeed("Water Speed", Range( 0 , 1)) = 0
		_WaterDepth("Water Depth", Range( 0 , 1)) = 0
		_WaterShallowColor("Water ShallowColor", Color) = (0,0,0,0)
		_WaterDeepColor("Water DeepColor", Color) = (1,1,1,0)
		_WaterFalloff("Water Falloff", Range( 0 , 10)) = 0
		_WaterSpecular("Water Specular", Range( 0 , 1)) = 0
		_WaterSmoothness("Water Smoothness", Float) = 0
		_WaterReflection("Water Reflection", Range( 0 , 1)) = 0
		_Foam_Texture("Foam_Texture", 2D) = "white" {}
		_FoamDepth("Foam Depth", Range( 0 , 10)) = 0
		_FoamFalloff("Foam Falloff", Float) = 0
		_FoamSmoothness("Foam Smoothness", Float) = 0
		_WaterOpacity("Water Opacity", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma instancing_options procedural:setup
		#pragma multi_compile GPU_FRUSTUM_ON __
		#include "VS_indirect.cginc"
		#pragma surface surf StandardSpecular alpha:fade keepalpha 
		struct Input
		{
			half2 uv_texcoord;
			float4 screenPos;
		};

		uniform half _WaterScale;
		uniform sampler2D _WaterNormal;
		uniform half _WaterSpeed;
		uniform float4 _WaterNormal_ST;
		uniform half4 _WaterShallowColor;
		uniform half4 _WaterDeepColor;
		uniform sampler2D _CameraDepthTexture;
		uniform half _WaterDepth;
		uniform half _WaterFalloff;
		uniform half _FoamDepth;
		uniform half _FoamFalloff;
		uniform sampler2D _Foam_Texture;
		uniform float4 _Foam_Texture_ST;
		uniform half _WaterSpecular;
		uniform half _WaterSmoothness;
		uniform half _FoamSmoothness;
		uniform half _WaterReflection;
		uniform half _WaterOpacity;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			half2 temp_cast_0 = (_WaterSpeed).xx;
			float2 uv_WaterNormal = i.uv_texcoord * _WaterNormal_ST.xy + _WaterNormal_ST.zw;
			float2 panner19 = ( 1.0 * _Time.y * temp_cast_0 + uv_WaterNormal);
			o.Normal = UnpackScaleNormal( tex2D( _WaterNormal, panner19 ), _WaterScale );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float eyeDepth1 = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD( ase_screenPos ))));
			float temp_output_89_0 = abs( ( eyeDepth1 - ase_screenPos.w ) );
			float4 lerpResult13 = lerp( _WaterShallowColor , _WaterDeepColor , saturate( pow( ( temp_output_89_0 + _WaterDepth ) , _WaterFalloff ) ));
			float2 uv_Foam_Texture = i.uv_texcoord * _Foam_Texture_ST.xy + _Foam_Texture_ST.zw;
			float2 panner116 = ( 1.0 * _Time.y * float2( -0.01,0.01 ) + uv_Foam_Texture);
			float temp_output_114_0 = ( saturate( pow( ( temp_output_89_0 + _FoamDepth ) , _FoamFalloff ) ) * tex2D( _Foam_Texture, panner116 ).r );
			float4 lerpResult117 = lerp( lerpResult13 , half4(1,1,1,0) , temp_output_114_0);
			o.Albedo = lerpResult117.rgb;
			float temp_output_104_0 = _WaterSpecular;
			half3 temp_cast_2 = (temp_output_104_0).xxx;
			o.Specular = temp_cast_2;
			float lerpResult133 = lerp( _WaterSmoothness , _FoamSmoothness , temp_output_114_0);
			o.Smoothness = ( lerpResult133 * _WaterReflection );
			o.Alpha = _WaterOpacity;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16200
2567;32;2546;1397;1330.299;671.4307;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;152;-1433.498,-615.4986;Float;False;828.5967;315.5001;Screen Depth;4;89;2;1;3;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;2;-1397.798,-511.9988;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenDepthNode;1;-1161.497,-514.499;Float;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;153;-576.1042,-114.6821;Float;False;915.4021;475.1005;Foam;9;114;113;110;115;105;116;106;112;111;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;3;-954.0988,-469.1988;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;159;-577.501,-617.1004;Float;False;918.2009;486.2004;Water Depth;8;13;94;11;87;12;88;10;6;;1,1,1,1;0;0
Node;AmplifyShaderEditor.AbsOpNode;89;-768.9045,-471.3828;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-553.7032,-52.18102;Float;False;Property;_FoamDepth;Foam Depth;11;0;Create;True;0;0;False;0;0;0.9;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-551.7026,32.41899;Float;False;Property;_FoamFalloff;Foam Falloff;12;0;Create;True;0;0;False;0;0;-27.57;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-539.6958,-288.6396;Float;False;Property;_WaterDepth;Water Depth;3;0;Create;True;0;0;False;0;0;0.28;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;115;-247.5019,-70.18164;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;106;-546.9041,191.8202;Float;False;0;105;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;116;-280.7024,195.1196;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.01,0.01;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;110;-98.60252,-70.08109;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;88;-272.0506,-387.7826;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-532.8712,-212.8153;Float;False;Property;_WaterFalloff;Water Falloff;6;0;Create;True;0;0;False;0;0;1.09;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;113;56.99892,-69.48123;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;105;-74.30196,160.1194;Float;True;Property;_Foam_Texture;Foam_Texture;10;0;Create;True;0;0;False;0;None;37e6f91f3efb0954cbdce254638862ea;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;151;-574.9057,-1078.484;Float;False;916.603;446.1994;Water Movement;5;17;19;48;166;21;;1,1,1,1;0;0
Node;AmplifyShaderEditor.PowerNode;87;-91.16573,-266.153;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;963.1613,-335.5159;Float;False;Property;_WaterSmoothness;Water Smoothness;8;0;Create;True;0;0;False;0;0;-0.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;132;966.7835,-242.4849;Float;False;Property;_FoamSmoothness;Foam Smoothness;13;0;Create;True;0;0;False;0;0;-0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;114;202.999,-69.08084;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;21;-524.9058,-955.6854;Float;False;0;17;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;12;-519.4658,-562.4554;Float;False;Property;_WaterShallowColor;Water ShallowColor;4;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;166;-526.252,-822.3657;Float;False;Property;_WaterSpeed;Water Speed;2;0;Create;True;0;0;False;0;0;0.01;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;94;82.19566,-259.584;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;11;-106.4798,-454.7199;Float;False;Property;_WaterDeepColor;Water DeepColor;5;0;Create;True;0;0;False;0;1,1,1,0;0.1260273,0.5305975,0.8161765,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;13;188.1001,-539.2983;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;133;1291.597,-284.68;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;170;1267.408,-125.2565;Float;False;Property;_WaterReflection;Water Reflection;9;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;108;374.9389,-744.7383;Float;False;Constant;_Color0;Color 0;-1;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;19;-197.7061,-981.3851;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.04,0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-526.5065,-732.2865;Float;False;Property;_WaterScale;Water Scale;1;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;117;782.4917,-746.7291;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;130;1179.167,527.939;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;167;1340.651,-423.7415;Float;False;Property;_WaterOpacity;Water Opacity;14;0;Create;True;0;0;False;0;0;0.217;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;131;791.0029,568.6337;Float;False;Constant;_FoamSpecular;Foam Specular;12;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;1559.911,-305.0965;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;17;42.69461,-1024.285;Float;True;Property;_WaterNormal;Water Normal;0;0;Create;True;0;0;False;0;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;104;994.7629,-669.7719;Float;False;Property;_WaterSpecular;Water Specular;7;0;Create;True;0;0;False;0;0;-1.55;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1715.601,-737.1998;Half;False;True;2;Half;ASEMaterialInspector;0;0;StandardSpecular;SyntyStudios/Water;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;3;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;3;Pragma;instancing_options procedural:setup;False;;Pragma;multi_compile GPU_FRUSTUM_ON __;False;;Include;VS_indirect.cginc;False;;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;1;0;2;0
WireConnection;3;0;1;0
WireConnection;3;1;2;4
WireConnection;89;0;3;0
WireConnection;115;0;89;0
WireConnection;115;1;111;0
WireConnection;116;0;106;0
WireConnection;110;0;115;0
WireConnection;110;1;112;0
WireConnection;88;0;89;0
WireConnection;88;1;6;0
WireConnection;113;0;110;0
WireConnection;105;1;116;0
WireConnection;87;0;88;0
WireConnection;87;1;10;0
WireConnection;114;0;113;0
WireConnection;114;1;105;1
WireConnection;94;0;87;0
WireConnection;13;0;12;0
WireConnection;13;1;11;0
WireConnection;13;2;94;0
WireConnection;133;0;26;0
WireConnection;133;1;132;0
WireConnection;133;2;114;0
WireConnection;19;0;21;0
WireConnection;19;2;166;0
WireConnection;117;0;13;0
WireConnection;117;1;108;0
WireConnection;117;2;114;0
WireConnection;130;0;104;0
WireConnection;130;1;131;0
WireConnection;130;2;114;0
WireConnection;169;0;133;0
WireConnection;169;1;170;0
WireConnection;17;1;19;0
WireConnection;17;5;48;0
WireConnection;0;0;117;0
WireConnection;0;1;17;0
WireConnection;0;3;104;0
WireConnection;0;4;169;0
WireConnection;0;9;167;0
ASEEND*/
//CHKSM=E201A2DE6EBDBEC8E3CD9690EA27A7A87C948536