// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Based on builtin Internal-DepthNormalsTexture.shader
// EncodeDepthNormal() is replaced with custom Output() function

Shader "Hidden/SimpleReplacement" 
{
    Properties {
        _MainTex ("", 2D) = "white" {}
        _Cutoff ("", Float) = 0.5
        _Color ("", Color) = (1,1,1,1)

        _ObjectColor ("Object Color", Color) = (1,1,1,1)
        _CategoryColor ("Catergory Color", Color) = (0,1,0,1)
    }

    SubShader {
        CGINCLUDE

            fixed4 _ObjectColor;

            float4 Output(float depth01, float3 normal)
            {
                return _ObjectColor;
            }
        ENDCG

    // Support for different RenderTypes
    // The following code is based on builtin Internal-DepthNormalsTexture.shader
        Tags { "Queue"="Transparent" "Render"="Transparent" "IgnoreProjector"="True"}
        Pass {
            
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct v2f {
                    float4 pos : SV_POSITION;
                    float4 nz : TEXCOORD0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert( appdata_base v ) {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.nz.xyz = COMPUTE_VIEW_NORMAL;
                    o.nz.w = COMPUTE_DEPTH_01;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    return Output (i.nz.w, i.nz.xyz);
                }
            ENDCG
        }
    }

Fallback Off
}
