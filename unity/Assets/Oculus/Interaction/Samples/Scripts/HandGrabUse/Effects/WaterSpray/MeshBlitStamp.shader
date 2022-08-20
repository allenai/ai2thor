/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "MeshBlit/MeshBlitStamp"
{
	Properties
	{
		[NoScaleOffset] _Stamp("Stamp", 2D) = "black" {}
		_StampMultipler("Stamp Multipler", Float) = 1
		[Enum(UV0,0,UV1,1)] _UV("UV Set", Float) = 1

		[HideInInspector]_MainTex("Texture", 2D) = "black" {}
		[HideInInspector]_Subtract("Subtract", float) = 0
	}
		SubShader
		{
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float4 normal : NORMAL;
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 stampUV : TEXCOORD1;
					float3 normalWS : TEXCOORD4;
					float4 vertex : SV_POSITION;
				};

				float4x4 _StampMatrix;
				sampler2D _MainTex;
				sampler2D _Stamp;
				half _StampMultipler;
				float _Subtract;
				half _UV;

				v2f vert(appdata v)
				{
					v2f o;
					float2 uv = (_UV == 0) ? v.uv0 : v.uv1;
					o.uv = uv;
	#if SHADER_API_D3D11
					uv.y = 1 - uv.y;
	#endif
					o.vertex = float4(uv * 2 - 1, 0.5, 1);
					float4 positionWS = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
					o.stampUV = mul(_StampMatrix, positionWS);
					o.normalWS = normalize(UnityObjectToWorldNormal(v.normal)); //TODO dont include the stamp on backfaces
					return o;
				}

				half4 frag(v2f i) : SV_Target
				{
					float4 col = tex2D(_MainTex, i.uv);
					col = max(0, col - _Subtract);

					float2 stampUV = saturate((i.stampUV.xy / i.stampUV.w) * 0.5 + 0.5);
					half4 stamp = tex2D(_Stamp, stampUV);
					col += stamp * _StampMultipler;
					col = saturate(col);
					return col;
				}
				ENDCG
			}
		}
}
