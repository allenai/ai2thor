/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Oculus/Interaction/Room"
{
		Properties
		{
			_MainTex("MainTex", 2D) = "white" {}
            _DetailTex("Detail Texture", 2D) = "white" {}
            _DetailTexIntensity("Detail Texture Intensity", Range(0, 1)) = 1

			_ColorLight("Light Color", Color) = (1, 1, 1, 1)
			_ColorDark("Dark Color", Color) = (0, 0, 0, 1)
			_DitherStrength("Dither Strength", int) = 16

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

		SubShader
		{
			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0"}
			LOD 100

			CGINCLUDE
			#pragma target 3.0
			ENDCG

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
				#include "InteractionCG.cginc"

				struct VertexInput
				{
					float4 vertex : POSITION;
					half2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct VertexOutput
				{
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform sampler2D _MainTex;
				uniform half4 _MainTex_ST;
                uniform sampler2D _DetailTex;
                uniform half _DetailTexIntensity;
                uniform half4 _DetailTex_ST;
				uniform half4 _ColorLight;
                uniform half4 _ColorDark;

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

				half4 frag(VertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                    half ditherNoise = DitherAnimatedNoise(i.vertex.xy);
					half4 mainTexture = tex2D(_MainTex, i.texcoord.xy);
					half4 detailTexture =
                        tex2D(_DetailTex, (i.texcoord.xy * _DetailTex_ST.xy) + _DetailTex_ST.zw);
                    detailTexture = saturate(pow(detailTexture, _DetailTexIntensity));
					half4 finalColor = lerp(_ColorDark, _ColorLight, (mainTexture.r* detailTexture.r) + ditherNoise);
					UNITY_OPAQUE_ALPHA(finalColor.a);
                    return finalColor;
				}
				ENDCG
			}
	}
	FallBack "Diffuse"
}
