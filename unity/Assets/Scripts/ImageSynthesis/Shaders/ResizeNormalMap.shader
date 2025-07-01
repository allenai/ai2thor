Shader "Custom/ResizeNormalMap"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        Pass {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0) {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                float3 normalRGB = tex2D(_MainTex, i.uv).rgb;
                float3 normal = normalize(normalRGB * 2.0 - 1.0);
                normal = normal * 0.5 + 0.5; // encode
                return float4(normal, 1);
            }
            ENDCG
        }
    }
}
