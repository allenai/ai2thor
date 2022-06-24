Shader "Custom/ScreenFade"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _FadeColor("Color", Color) = (0,0,0,0)
        _Alpha("Alpha", Range(0.0, 1.0)) = 0
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

                struct AppToVertex
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct VertexToFragment
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                VertexToFragment vert(AppToVertex v)
                {
                    VertexToFragment o;

                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(VertexToFragment, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
                sampler2D _MaskTex;
                half4 _FadeColor;
                half _Alpha;

                fixed3 frag(VertexToFragment i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                    fixed4 color = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
                    fixed3 lerpedColor = lerp(color.rgb, _FadeColor.rgb, _Alpha);
                    return lerpedColor;
                }
                ENDCG
            }
        }
}