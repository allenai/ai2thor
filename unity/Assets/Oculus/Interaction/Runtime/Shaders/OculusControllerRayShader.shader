/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/OculusControllerRayShader"
{
    Properties
    {
        _Color0 ("Color0", Color) = (1,1,1,1)
        _Color1 ("Color1", Color) = (1,1,1,0)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.zz * 2.0f;
                return o;
            }

            uniform float4 _Color0;
            uniform float4 _Color1;

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = lerp(_Color0, _Color1, clamp(i.uv.x, 0.0f, 1.0f) * 1.5f);
                return col;
            }
            ENDCG
        }
    }
}
