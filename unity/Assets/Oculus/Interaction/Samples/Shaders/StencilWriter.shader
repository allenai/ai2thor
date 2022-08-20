/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Oculus/Interaction/StencilWriter"
{
	Properties
	{
		[IntRange] _StencilRef("Stencil Reference Value", Range(0,255)) = 0
	}

	SubShader{

		Tags {"Queue" = "Geometry+501"} // stuck way up here so that it sorts with transparent objects

		ZWrite Off

		ColorMask 0 // Don't write to any colour channels
		Stencil
		{
			Ref[_StencilRef]
			Comp Always
			Pass Replace
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct VertexInput
			{
				half4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				half4 pos : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			half4 frag(VertexOutput i) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return 0;
			}

			ENDCG
		}
	}
}
