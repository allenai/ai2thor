/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Oculus/Interaction/deskShadow"
{
		Properties
		{
			_MainTex("MainTex", 2D) = "white" {}
			_Color("Shadow Color", Color) = (0, 0, 0, 0)

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

		SubShader
		{
			Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0"}
			LOD 100

			CGINCLUDE
			#pragma target 3.0
			ENDCG
			Blend DstColor Zero

			Pass
			{
				Name "Base"
				CGPROGRAM

				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				//only defining to not throw compilation error over Unity 5.5
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing

				#include "UnityCG.cginc"
				#include "UnityShaderVariables.cginc"

				struct vertexInput
				{
					float4 vertex : POSITION;
					half2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct vertexOutput
				{
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
                uniform half4 _Color;

				vertexOutput vert(vertexInput v)
				{
					vertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord;
					return o;
				}

				fixed4 frag(vertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
					half4 mainTexture = tex2D(_MainTex, i.texcoord.xy);
					half4 finalColor = lerp (_Color, half4(1, 1, 1, 1), mainTexture.r) ;
                    return finalColor;
				}
				ENDCG
			}
	}
}
