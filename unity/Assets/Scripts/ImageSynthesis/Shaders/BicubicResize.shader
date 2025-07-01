Shader "Custom/BicubicResize"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            float cubic(float v) {
                v = abs(v);
                if (v < 1.0) return 1.0 - 2.0 * v * v + v * v * v;
                if (v < 2.0) return 4.0 - 8.0 * v + 5.0 * v * v - v * v * v;
                return 0.0;
            }

            float4 sampleBicubic(float2 uv) {
                float2 texSize = 1.0 / _MainTex_TexelSize.xy;
                float2 coord = uv * texSize - 0.5;
                float2 baseCoord = floor(coord);
                float2 fractCoord = coord - baseCoord;

                float4 color = float4(0,0,0,0);
                for (int m = -1; m <= 2; m++) {
                    for (int n = -1; n <= 2; n++) {
                        float2 offset = float2(n, m);
                        float2 samplePos = (baseCoord + offset + 0.5) * _MainTex_TexelSize.xy;
                        float weight = cubic(n - fractCoord.x) * cubic(m - fractCoord.y);
                        color += tex2D(_MainTex, samplePos) * weight;
                    }
                }
                return color;
            }

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return sampleBicubic(i.uv);
            }
            ENDCG
        }
    }
}
