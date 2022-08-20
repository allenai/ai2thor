/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Oculus/Interaction/UIStyle"
{
		Properties
		{
			_Color("Color", Color) = (1,1,1,1)

			[HideInInspector] _texcoord("", 2D) = "white" {}
		}

		SubShader
		{
			Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0"}
			LOD 100

			CGINCLUDE
			#pragma target 3.0
			ENDCG
			Blend SrcAlpha OneMinusSrcAlpha

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

				struct VertexInput
				{
					float4 vertex : POSITION;
                    half4 vertexColor : COLOR;
                    half3 normal : NORMAL;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct VertexOutput
				{
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
                    half3 normal : TEXCOORD2;
                    half4 vertexColor : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform half4 _Color;

				VertexOutput vert(VertexInput v)
				{
					VertexOutput o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
                    o.normal = UnityObjectToWorldNormal(v.normal);
                    half pulse = sin(_Time.z) * 0.5 + 0.5;
					float4 vertexPos = UnityObjectToClipPos(v.vertex);
                    vertexPos.xyz = vertexPos + ((0.002 * pulse) * o.normal * v.vertexColor.a);
                    o.vertex = vertexPos;
                    o.vertexColor = v.vertexColor;
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

					return o;
				}

				half4 frag(VertexOutput i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                    float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                    half3 worldNormal = i.normal;
                    half fresnel = saturate(dot(worldViewDir, worldNormal));
                    fresnel = saturate(pow(fresnel + .1, 3) * 2);

					half opacity =  fresnel * i.vertexColor.a;

					half4 debug = half4(i.vertexColor.a, i.vertexColor.a, i.vertexColor.a, 1.0);

					half4 finalColor = _Color * i.vertexColor;
                    return half4(finalColor.rgb, opacity);
				}
				ENDCG
			}




	}
}
