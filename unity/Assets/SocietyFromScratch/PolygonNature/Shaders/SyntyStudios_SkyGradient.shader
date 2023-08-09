// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/SkyGradient"
{
	Properties
	{
		_ColorTop("Color Top", Color) = (0,1,0.7517242,0)
		_ColorBottom("Color Bottom", Color) = (0,1,0.7517242,0)
		_Offset("Offset", Float) = 0
		_Distance("Distance", Float) = 0
		_Falloff("Falloff", Range( 0.001 , 100)) = 0.001
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma instancing_options procedural:setup
		#pragma multi_compile GPU_FRUSTUM_ON __
		#include "VS_indirect.cginc"
		#pragma surface surf Unlit keepalpha noshadow nofog 
		struct Input
		{
			float3 worldPos;
		};

		uniform float4 _ColorBottom;
		uniform float4 _ColorTop;
		uniform float _Offset;
		uniform float _Distance;
		uniform float _Falloff;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float3 ase_worldPos = i.worldPos;
			float clampResult40 = clamp( ( ( _Offset + ase_worldPos.y ) / _Distance ) , 0.0 , 1.0 );
			float4 lerpResult11 = lerp( _ColorBottom , _ColorTop , saturate( pow( clampResult40 , _Falloff ) ));
			o.Emission = lerpResult11.rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16200
2567;32;2546;1397;1176.147;474.3083;1;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;50;-452.7357,211.3484;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;42;-424.9747,105.4285;Float;False;Property;_Offset;Offset;2;0;Create;True;0;0;False;0;0;38.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-359.3628,379.4822;Float;False;Property;_Distance;Distance;3;0;Create;True;0;0;False;0;0;44.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;43;-155.9747,185.4285;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;39;-135.9925,325.9052;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-83.40302,564.1992;Float;False;Property;_Falloff;Falloff;4;0;Create;True;0;0;False;0;0.001;1;0.001;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;40;11.642,208.1304;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;41;288.2426,279.4253;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-337.2385,-283.9739;Float;False;Property;_ColorBottom;Color Bottom;1;0;Create;True;0;0;False;0;0,1,0.7517242,0;0.0116782,0.03972877,0.06617647,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;18;-335.864,-108.367;Float;False;Property;_ColorTop;Color Top;0;0;Create;True;0;0;False;0;0,1,0.7517242,0;0.691,1,1,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;32;172.9571,104.6199;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;11;114.7418,-102.6559;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;49;391.0693,-233.56;Float;False;True;2;Float;ASEMaterialInspector;0;0;Unlit;SyntyStudios/SkyGradient;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;3;Pragma;instancing_options procedural:setup;False;;Pragma;multi_compile GPU_FRUSTUM_ON __;False;;Include;VS_indirect.cginc;False;;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;43;0;42;0
WireConnection;43;1;50;2
WireConnection;39;0;43;0
WireConnection;39;1;37;0
WireConnection;40;0;39;0
WireConnection;41;0;40;0
WireConnection;41;1;38;0
WireConnection;32;0;41;0
WireConnection;11;0;2;0
WireConnection;11;1;18;0
WireConnection;11;2;32;0
WireConnection;49;2;11;0
ASEEND*/
//CHKSM=DE7D3B73551FE6781407EFBDBC82E3FEE5B78507