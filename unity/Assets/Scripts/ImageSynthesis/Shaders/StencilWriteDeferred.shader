Shader "Unlit/StencilWriteShader"
{
   
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode" = "Deferred" "Queue"="Geometry+225"}

        Stencil {
            Ref 1
            Comp Always
            WriteMask 255
            Pass Replace
            ZFail Replace
        }

        Pass
        {
            Blend Zero One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}
