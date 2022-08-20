/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Hidden/DepthOverwrite" {
    Properties { 
        _Offset ("Offset (m)", Float) = 0.1
    }
    SubShader {
        Tags { "Queue"="Transparent+2000" "RenderType"="Opaque" }
        LOD 100

        Cull Off
        ColorMask 0

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Offset;

            v2f vert (appdata v) {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
                float3 toCamera = normalize(_WorldSpaceCameraPos - worldPos);
                float3 adjustedPos = worldPos + toCamera * _Offset;

                o.vertex = mul(UNITY_MATRIX_VP, float4(adjustedPos, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return float4(1, 0, 0, 1);
            }
            ENDCG
        }
    }
}
