// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/Vines"
{
	Properties
	{
		_MainTexture("_MainTexture", 2D) = "white" {}
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Wave("Wave", Range( 0 , 10)) = 0
		_WindSpeed("WindSpeed", Float) = 0
		_Amount("Amount", Float) = 1
		_ColorTint("_ColorTint", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "DisableBatching" = "True" }
		Cull Off
		Stencil
		{
			Ref 0
		}
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma instancing_options procedural:setup
		#pragma multi_compile GPU_FRUSTUM_ON __
		#include "VS_indirect.cginc"
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _TextureSample0;
		uniform float _WindSpeed;
		uniform float _Wave;
		uniform float _Amount;
		uniform sampler2D _MainTexture;
		uniform float4 _MainTexture_ST;
		uniform float4 _ColorTint;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 temp_cast_0 = (( ( _Time.y * _WindSpeed ) / ( 1.0 - _Wave ) )).xx;
			float4 temp_cast_1 = (_SinTime.x).xxxx;
			float4 lerpResult167 = lerp( ( tex2Dlod( _TextureSample0, float4( temp_cast_0, 0, 0.0) ) * _Amount ) , temp_cast_1 , float4( 0,0,0,0 ));
			float4 lerpResult143 = lerp( lerpResult167 , float4( 0,0,0,0 ) , v.color.r);
			float3 appendResult160 = (float3(( lerpResult143 / -60.0 ).rgb));
			v.vertex.xyz += float3( (appendResult160).xz ,  0.0 );
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_MainTexture = i.uv_texcoord * _MainTexture_ST.xy + _MainTexture_ST.zw;
			float4 tex2DNode2 = tex2D( _MainTexture, uv_MainTexture );
			o.Albedo = ( tex2DNode2 * _ColorTint ).rgb;
			o.Alpha = 1;
			clip( tex2DNode2.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16200
2567;32;2546;1397;1284.749;368.2379;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;8;-816.794,50.04406;Float;False;1337.761;534.3865;Red Vertex;17;152;148;138;140;153;145;143;144;155;154;163;160;161;165;164;166;167;Vertex Animation;1,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-795.8595,475.9767;Float;False;Property;_Wave;Wave;3;0;Create;True;0;0;False;0;0;10;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-788.3361,342.2771;Float;False;Property;_WindSpeed;WindSpeed;4;0;Create;True;0;0;False;0;0;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;152;-796.587,252.8969;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;148;-622.6687,244.997;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;163;-499.749,482.2621;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;153;-376.8158,283.0264;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;145;-251.8585,385.1362;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;bdbe94d7623ec3940947b62544306f1c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;165;-219.749,289.2621;Float;False;Property;_Amount;Amount;5;0;Create;True;0;0;False;0;1;-60;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;164;-11.74902,262.2621;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SinTimeNode;166;-244.749,103.2621;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;167;-10.74902,125.2621;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;144;97.20142,351.7871;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;143;157.0519,118.9456;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;155;119.4311,503.7776;Float;False;Constant;_Float2;Float 2;5;0;Create;True;0;0;False;0;-60;-60;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;154;384.2011,118.1072;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;47.39497,-333.8198;Float;True;Property;_MainTexture;_MainTexture;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;160;371.251,230.2621;Float;False;FLOAT3;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;158;48.25085,-128.7379;Float;False;Property;_ColorTint;_ColorTint;6;0;Create;True;0;0;False;0;0,0,0,0;1,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;386.2509,-265.7379;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;161;305.251,346.2621;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;647.9548,-143.48;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;SyntyStudios/Vines;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Opaque;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;True;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;2;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;3;Pragma;instancing_options procedural:setup;False;;Pragma;multi_compile GPU_FRUSTUM_ON __;False;;Include;VS_indirect.cginc;False;;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;148;0;152;0
WireConnection;148;1;138;0
WireConnection;163;0;140;0
WireConnection;153;0;148;0
WireConnection;153;1;163;0
WireConnection;145;1;153;0
WireConnection;164;0;145;0
WireConnection;164;1;165;0
WireConnection;167;0;164;0
WireConnection;167;1;166;1
WireConnection;143;0;167;0
WireConnection;143;2;144;1
WireConnection;154;0;143;0
WireConnection;154;1;155;0
WireConnection;160;0;154;0
WireConnection;159;0;2;0
WireConnection;159;1;158;0
WireConnection;161;0;160;0
WireConnection;0;0;159;0
WireConnection;0;10;2;4
WireConnection;0;11;161;0
ASEEND*/
//CHKSM=752954C4C33E46F8E2AA0E481BACF1B40C7CB986