/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/OculusHand"
{
	Properties
	{
		[Header(General)]
		_ColorTop("Color Top", Color) = (0.1960784, 0.2039215, 0.2117647, 1)
		_ColorBottom("Color Bottom", Color) = (0.1215686, 0.1254902, 0.1294117, 1)
		_FingerGlowColor("Finger Glow Color", Color) = (1,1,1,1)
		_Opacity("Opacity", Range(0 , 1)) = 0.8

		[Header(Fresnel)]
		_FresnelPower("FresnelPower", Range(0 , 5)) = 0.16

		[Header(Outline)]
		_OutlineColor("Outline Color", Color) = (0.5377358,0.5377358,0.5377358,1)
		_OutlineJointColor("Outline Joint Error Color", Color) = (1,0,0,1)
		_OutlineWidth("Outline Width", Range(0 , 0.005)) = 0.00134
		_OutlineOpacity("Outline Opacity", Range(0 , 1)) = 0.4

		[Header(Wrist)]
		_WristFade("Wrist Fade", Range(0 , 1)) = 0.5

		[Header(Finger Glow)]
		_FingerGlowMask("Finger Glow Mask", 2D) = "white" {}
		[Toggle(CONFIDENCE)] _EnableConfidence("Show Low Confidence", Float) = 0

		[HideInInspector] _texcoord("", 2D) = "white" {}
	}

	CGINCLUDE
	#include "Lighting.cginc"
	#pragma target 3.0

	// General
	uniform float4 _ColorTop;
	uniform float4 _ColorBottom;
	uniform float _Opacity;
	uniform float _FresnelPower;

	// Outline
	uniform float4 _OutlineColor;
	uniform half4 _OutlineJointColor;
	uniform float _OutlineWidth;
	uniform float _OutlineOpacity;

	// Wrist
	uniform half _WristFade;

	// Finger Glow
	uniform sampler2D _FingerGlowMask;
	uniform float4 _FingerGlowColor;

	uniform float _ThumbGlowValue;
	uniform float _IndexGlowValue;
	uniform float _MiddleGlowValue;
	uniform float _RingGlowValue;
	uniform float _PinkyGlowValue;

	uniform half _JointsGlow[18];
	ENDCG

	SubShader
	{
		LOD 100
		Tags
		{
			"RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			Name "Interior"
			Tags
			{
				"RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "IsEmissive" = "true"
			}

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex baseVertex
			#pragma fragment baseFragment
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

			struct VertexInput
			{
				float4 vertex : POSITION;
				half3 normal : NORMAL;
				half4 vertexColor : COLOR;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				half4 glowColor : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput baseVertex(VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.vertex = UnityObjectToClipPos(v.vertex);

				half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);
				int glowMaskR = maskPixelColor.r * 255;

				int thumbMask = (glowMaskR >> 3) & 0x1;
				int indexMask = (glowMaskR >> 4) & 0x1;
				int middleMask = (glowMaskR >> 5) & 0x1;
				int ringMask = (glowMaskR >> 6) & 0x1;
				int pinkyMask = (glowMaskR >> 7) & 0x1;

				half glowIntensity = saturate(
					maskPixelColor.g *
					(thumbMask * _ThumbGlowValue
						+ indexMask * _IndexGlowValue
						+ middleMask * _MiddleGlowValue
						+ ringMask * _RingGlowValue
						+ pinkyMask * _PinkyGlowValue));

				half4 glow = glowIntensity * _FingerGlowColor;
				o.glowColor.rgb = glow.rgb;
				o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _Opacity;
				return o;
			}

			half4 baseFragment(VertexOutput i) : SV_Target
			{
				float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

				float fresnelNdot = dot(i.worldNormal, worldViewDir);
				float fresnel = 1.0 * pow(1.0 - fresnelNdot, _FresnelPower);
				float4 color = lerp(_ColorTop, _ColorBottom, fresnel);

				half3 glowColor = saturate(color + i.glowColor.rgb);
				return half4(glowColor, i.glowColor.a);
			}

			ENDCG
		}
	}
	
	SubShader
	{
		LOD 200
		Tags
		{
			"Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True"
		}

		Pass
		{
			Name "Depth"
			ZWrite On
			ColorMask 0
		}

		Pass
		{	
			Name "Outline"
			Tags
			{
				"RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"
			}
			Cull Front
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex outlineVertex
			#pragma fragment outlineFragment
			#pragma multi_compile_local __ CONFIDENCE

			struct OutlineVertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct OutlineVertexOutput
			{
				float4 vertex : SV_POSITION;
				half4 glowColor : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			OutlineVertexOutput outlineVertex(OutlineVertexInput v)
			{
				OutlineVertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				v.vertex.xyz += v.normal * _OutlineWidth;
				o.vertex = UnityObjectToClipPos(v.vertex);

				half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);
				
#if CONFIDENCE
				int glowMaskR = maskPixelColor.r * 255;
				int jointMaskB = maskPixelColor.b * 255;

				int thumbMask = (glowMaskR >> 3) & 0x1;
				int indexMask = (glowMaskR >> 4) & 0x1;
				int middleMask = (glowMaskR >> 5) & 0x1;
				int ringMask = (glowMaskR >> 6) & 0x1;
				int pinkyMask = (glowMaskR >> 7) & 0x1;

				int joint0 = (jointMaskB >> 4) & 0x1;
				int joint1 = (jointMaskB >> 5) & 0x1;
				int joint2 = (jointMaskB >> 6) & 0x1;
				int joint3 = (jointMaskB >> 7) & 0x1;

				half jointIntensity = saturate(
					((1 - saturate(glowMaskR)) * _JointsGlow[0])
					+ thumbMask * (joint0 * _JointsGlow[1]
						+ joint1 * _JointsGlow[2]
						+ joint2 * _JointsGlow[3]
						+ joint3 * _JointsGlow[4])
					+ indexMask * (joint1 * _JointsGlow[5]
						+ joint2 * _JointsGlow[6]
						+ joint3 * _JointsGlow[7])
					+ middleMask * (joint1 * _JointsGlow[8]
						+ joint2 * _JointsGlow[9]
						+ joint3 * _JointsGlow[10])
					+ ringMask * (joint1 * _JointsGlow[11]
						+ joint2 * _JointsGlow[12]
						+ joint3 * _JointsGlow[13])
					+ pinkyMask * (joint0 * _JointsGlow[14]
						+ joint1 * _JointsGlow[15]
						+ joint2 * _JointsGlow[16]
						+ joint3 * _JointsGlow[17]));

				half4 glow = lerp(_OutlineColor, _OutlineJointColor, jointIntensity);
				o.glowColor.rgb = glow.rgb;
				o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * glow.a * _OutlineOpacity;
#else
				o.glowColor.rgb = _OutlineColor;
				o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _OutlineColor.a * _OutlineOpacity;
#endif

				return o;
			}

			half4 outlineFragment(OutlineVertexOutput i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				return i.glowColor;
			}
			ENDCG
		}

		Pass
		{
			Name "Interior"
			Tags
			{
				"RenderType" = "MaskedOutline" "Queue" = "Transparent" "IgnoreProjector" = "True" "IsEmissive" = "true"
			}
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex baseVertex
			#pragma fragment baseFragment
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			struct VertexInput
			{
				float4 vertex : POSITION;
				half3 normal : NORMAL;
				half4 vertexColor : COLOR;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				half4 glowColor : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput baseVertex(VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.vertex = UnityObjectToClipPos(v.vertex);

				half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);
				int glowMaskR = maskPixelColor.r * 255;

				int thumbMask = (glowMaskR >> 3) & 0x1;
				int indexMask = (glowMaskR >> 4) & 0x1;
				int middleMask = (glowMaskR >> 5) & 0x1;
				int ringMask = (glowMaskR >> 6) & 0x1;
				int pinkyMask = (glowMaskR >> 7) & 0x1;

				half glowIntensity = saturate(
					maskPixelColor.g *
					(thumbMask * _ThumbGlowValue
						+ indexMask * _IndexGlowValue
						+ middleMask * _MiddleGlowValue
						+ ringMask * _RingGlowValue
						+ pinkyMask * _PinkyGlowValue));

				half4 glow = glowIntensity * _FingerGlowColor;
				o.glowColor.rgb = glow.rgb;
				o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _Opacity;
				return o;
			}

			half4 baseFragment(VertexOutput i) : SV_Target
			{
				float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				float fresnelNdot = dot(i.worldNormal, worldViewDir);
				float fresnel = 1.0 * pow(1.0 - fresnelNdot, _FresnelPower);
				float4 color = lerp(_ColorTop, _ColorBottom, fresnel);

				half3 glowColor = saturate(color + i.glowColor.rgb);
				return half4(glowColor, i.glowColor.a);
			}

			ENDCG
		}
	}
}
