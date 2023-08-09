// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/Trees"
{
	Properties
	{
		_Emission("Emission", 2D) = "white" {}
		_MainTexture("_MainTexture", 2D) = "white" {}
		_ColorTint("_ColorTint", Color) = (0,0,0,0)
		_EmissionColor("EmissionColor", Color) = (0,0,0,0)
		_Tree_NoiseTexture("Tree_NoiseTexture", 2D) = "white" {}
		_Big_Wave("Big_Wave", Range( 0 , 10)) = 0
		_Big_Windspeed("Big_Windspeed", Float) = 0
		_Big_WindAmount("Big_WindAmount", Float) = 1
		_Leaves_NoiseTexture("Leaves_NoiseTexture", 2D) = "white" {}
		_Small_Wave("Small_Wave", Range( 0 , 10)) = 0
		_Small_WindSpeed("Small_WindSpeed", Float) = 0
		_Small_WindAmount("Small_WindAmount", Float) = 1
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "DisableBatching" = "True" "IsEmissive" = "true"  }
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

		uniform float _Small_WindAmount;
		uniform sampler2D _Leaves_NoiseTexture;
		uniform float _Small_WindSpeed;
		uniform float _Small_Wave;
		uniform float _Big_WindAmount;
		uniform sampler2D _Tree_NoiseTexture;
		uniform float _Big_Windspeed;
		uniform float _Big_Wave;
		uniform sampler2D _MainTexture;
		uniform float4 _MainTexture_ST;
		uniform float4 _ColorTint;
		uniform sampler2D _Emission;
		uniform float4 _Emission_ST;
		uniform float4 _EmissionColor;
		uniform float _Cutoff = 0.5;


		float4 CalculateContrast( float contrastValue, float4 colorTarget )
		{
			float t = 0.5 * ( 1.0 - contrastValue );
			return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
		}

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float2 temp_cast_0 = (( ( ase_vertex3Pos.x + ( _Time.y * _Small_WindSpeed ) ) / ( 1.0 - _Small_Wave ) )).xx;
			float lerpResult143 = lerp( tex2Dlod( _Leaves_NoiseTexture, float4( temp_cast_0, 0, 0.0) ).r , 0.0 , v.color.r);
			float3 appendResult160 = (float3(lerpResult143 , 0.0 , 0.0));
			float2 temp_cast_2 = (( ( _Time.y * _Big_Windspeed ) / ( 1.0 - _Big_Wave ) )).xx;
			float lerpResult170 = lerp( ( _Big_WindAmount * tex2Dlod( _Tree_NoiseTexture, float4( temp_cast_2, 0, 0.0) ).r ) , 0.0 , v.color.b);
			float3 appendResult172 = (float3(lerpResult170 , 0.0 , 0.0));
			v.vertex.xyz += ( CalculateContrast(_Small_WindAmount,float4( (appendResult160).xz, 0.0 , 0.0 )) + float4( (appendResult172).xz, 0.0 , 0.0 ) ).rgb;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_MainTexture = i.uv_texcoord * _MainTexture_ST.xy + _MainTexture_ST.zw;
			float4 tex2DNode2 = tex2D( _MainTexture, uv_MainTexture );
			o.Albedo = ( tex2DNode2 * _ColorTint ).rgb;
			float2 uv_Emission = i.uv_texcoord * _Emission_ST.xy + _Emission_ST.zw;
			o.Emission = ( tex2D( _Emission, uv_Emission ) * _EmissionColor ).rgb;
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
2567;32;2546;1397;1371.33;101.9897;1.07;True;False
Node;AmplifyShaderEditor.CommentaryNode;8;-881.794,235.0441;Float;False;1337.761;534.3865;Red Vertex;15;135;136;152;148;138;140;153;145;143;144;163;160;161;194;193;Leaves Vertex Animation;1,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleTimeNode;152;-860.587,437.8969;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;167;-878.4194,802.28;Float;False;1333.21;549.45;Blue Vertex;13;169;170;172;173;183;184;185;186;187;188;189;190;191;Tree Vertex Animation;0,0.3379312,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-859.3361,576.2771;Float;False;Property;_Small_WindSpeed;Small_WindSpeed;10;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;187;-828.7344,966.4954;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;148;-634.7638,436.1489;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;135;-865.7274,289.677;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;188;-830.7344,1126.495;Float;False;Property;_Big_Windspeed;Big_Windspeed;6;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-860.8595,679.9767;Float;False;Property;_Small_Wave;Small_Wave;9;0;Create;True;0;0;False;0;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;185;-833.1409,1253.858;Float;False;Property;_Big_Wave;Big_Wave;5;0;Create;True;0;0;False;0;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;189;-549.7344,1038.495;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;186;-526.7344,1243.495;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;136;-485.1785,309.377;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;163;-558.629,685.0221;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;184;-384.4278,1059.54;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;153;-382.1558,471.2664;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;191;-193.5998,1023.172;Float;False;Property;_Big_WindAmount;Big_WindAmount;7;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;145;-241.9585,568.3162;Float;True;Property;_Leaves_NoiseTexture;Leaves_NoiseTexture;8;0;Create;False;0;0;False;0;None;bdbe94d7623ec3940947b62544306f1c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;144;-246.7986,295.7871;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;183;-205.3401,1125.166;Float;True;Property;_Tree_NoiseTexture;Tree_NoiseTexture;4;0;Create;False;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;169;-386.5864,853.6269;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;143;47.41158,292.7952;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;190;83.13558,994.1928;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;160;259.461,307.3114;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;170;258.1481,839.1577;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;161;223.0009,448.0907;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;172;266.932,1064.721;Float;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;193;140.2306,666.5669;Float;False;Property;_Small_WindAmount;Small_WindAmount;11;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;194;219.7598,535.1939;Float;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;12.04499,-535.8204;Float;True;Property;_MainTexture;_MainTexture;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;165;-1.875582,-154.8326;Float;True;Property;_Emission;Emission;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;173;232.5317,1260.464;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;164;52.87036,54.28944;Float;False;Property;_EmissionColor;EmissionColor;3;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;158;45.22086,-336.798;Float;False;Property;_ColorTint;_ColorTint;2;0;Create;True;0;0;False;0;0,0,0,0;1,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;174;534.3312,737.3227;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;380.1908,-504.0986;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;166;365.8704,29.28943;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;647.9548,-143.48;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;SyntyStudios/Trees;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Opaque;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;True;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;12;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;3;Pragma;instancing_options procedural:setup;False;;Pragma;multi_compile GPU_FRUSTUM_ON __;False;;Include;VS_indirect.cginc;False;;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;148;0;152;0
WireConnection;148;1;138;0
WireConnection;189;0;187;0
WireConnection;189;1;188;0
WireConnection;186;0;185;0
WireConnection;136;0;135;1
WireConnection;136;1;148;0
WireConnection;163;0;140;0
WireConnection;184;0;189;0
WireConnection;184;1;186;0
WireConnection;153;0;136;0
WireConnection;153;1;163;0
WireConnection;145;1;153;0
WireConnection;183;1;184;0
WireConnection;143;0;145;1
WireConnection;143;2;144;1
WireConnection;190;0;191;0
WireConnection;190;1;183;1
WireConnection;160;0;143;0
WireConnection;170;0;190;0
WireConnection;170;2;169;3
WireConnection;161;0;160;0
WireConnection;172;0;170;0
WireConnection;194;1;161;0
WireConnection;194;0;193;0
WireConnection;173;0;172;0
WireConnection;174;0;194;0
WireConnection;174;1;173;0
WireConnection;159;0;2;0
WireConnection;159;1;158;0
WireConnection;166;0;165;0
WireConnection;166;1;164;0
WireConnection;0;0;159;0
WireConnection;0;2;166;0
WireConnection;0;10;2;4
WireConnection;0;11;174;0
ASEEND*/
//CHKSM=374444577852D46033DB6369F1D6DD35EDBB026A