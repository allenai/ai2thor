/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Oculus/Interaction/SkyboxGradient" 
{
	Properties
	{
		_TopColor("Top Color", Color) = (1, 0.3, 0.3, 1)
		_MiddleColor("MiddleColor", Color) = (1.0, 1.0, 1)
		_BottomColor("Bottom Color", Color) = (0.3, 0.3, 1, 1)
		_Direction("Direction", Vector) = (0, 1, 0)

		_DitherStrength("Dither Strength", int) = 16
	}

	SubShader
	{
		Tags 
		{
			"RenderType" = "Background"
			"Queue" = "Background"
		}

		Pass 
		{
			ZWrite Off
			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "InteractionCG.cginc"

			uniform half3 _TopColor;
			uniform half3 _BottomColor;
			uniform half3 _MiddleColor;
			uniform float3 _Direction;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;
				float3 texcoord : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			half4 frag(VertexOutput i) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 texcoord = normalize(i.texcoord);
				half ditherNoise = DitherAnimatedNoise(i.vertex.xy);

				float range = dot(texcoord, _Direction) + ditherNoise;

				half bottomRange = saturate(-range);
				half middleRange = 1 - abs(range);
				half topRange = saturate(range);

				half3 finalColor = _BottomColor.rgb * bottomRange
					+ _MiddleColor.rgb * middleRange
					+_TopColor.rgb * topRange;
				
				return half4(finalColor,1);
			}
			ENDCG
		}
	}
}
