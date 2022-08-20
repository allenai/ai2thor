/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Custom/PolylineUnlit" {

    Properties { }

    SubShader
    {

        Tags { "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "./CubePointToSegment.cginc"
            #include "../ThirdParty/Shaders/CapsuleRayIntersect.cginc"

            #if SHADER_TARGET >= 45
            StructuredBuffer<float4> _PositionBuffer;
            StructuredBuffer<float4> _ColorBuffer;
            #endif

            struct appdata_full_instance : appdata_full
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                sample float3 worldPos : TEXCOORD0;
                float4 p0 : TEXCOORD1;
                float4 p1 : TEXCOORD2;
                float4 col0 : TEXCOORD3;
                float4 col1 : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Scale;
            float4x4 _LocalToWorld;

            v2f vert(appdata_full_instance v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if SHADER_TARGET >= 45
                    float4 p0 = _PositionBuffer[instanceID * 2];
                    float4 p1 = _PositionBuffer[instanceID * 2 + 1];
                    float4 col0 = _ColorBuffer[instanceID * 2];
                    float4 col1 = _ColorBuffer[instanceID * 2 + 1];
                #else
                    float4 p0 = 0;
                    float4 p1 = 0;
                    float4 col0 = 0;
                    float4 col1 = 0;
                #endif

                float3 localPos = orientCubePointToSegmentWithWidth(v.vertex.xyz, p0.xyz, p1.xyz, p0.w, p0.w);
                float3 worldPos = mul(_LocalToWorld, float4(localPos, 1.0)).xyz;

                // Apply VP matrix to model
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos = worldPos;
                o.p0 = float4(mul(_LocalToWorld, float4(p0.xyz, 1.0)).xyz, p0.w);
                o.p1 = float4(mul(_LocalToWorld, float4(p1.xyz, 1.0)).xyz, p1.w);
                o.col0 = col0;
                o.col1 = col1;

                return o;
            }

            fixed4 frag (v2f i, out float out_depth : SV_Depth) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 rayDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
                float dist = capIntersect(_WorldSpaceCameraPos.xyz, rayDir, i.p0, i.p1,
                                        i.p0.w/2.0f * _Scale); // hardcoded sphere at 0,0,0 radius .5
                clip(dist);

                // calculate world space hit position
                float3 hitPos = _WorldSpaceCameraPos.xyz + rayDir * dist;

                // set output depth
                float4 clipPos = UnityWorldToClipPos(hitPos);

                out_depth =  clipPos.z / clipPos.w;

                #if !defined(UNITY_REVERSED_Z)
                out_depth = out_depth * 0.5 + 0.5;
                #endif

                float3 vec = i.p1.xyz - i.p0.xyz;
                float dotvecvec = dot(vec, vec);
                float t = 0.0f;
                if(abs(dotvecvec) > 0.0f)
                {
                    float3 toHit = hitPos - i.p0.xyz;
                    t = dot(toHit, vec)/dotvecvec;
                }
                return float4(lerp(i.col0, i.col1, t).rgb, 1.0f);
            }
            ENDCG
        }

    }
}
