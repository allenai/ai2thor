Shader "Hidden/DepthBW" {
     Properties
     {
         _MainTex ("Base (RGB)", 2D) = "white" {}
         _DepthLevel ("Depth Level", Range(1, 3)) = 1
     }
     SubShader
     {
         Pass
         {
             CGPROGRAM

              #pragma vertex vert
             #pragma fragment frag
             #include "UnityCG.cginc"

              uniform sampler2D _MainTex;
             uniform sampler2D _CameraDepthTexture;
             uniform fixed _DepthLevel;
             uniform half4 _MainTex_TexelSize;
            //  uniform float4 _ProjectionParams;

              struct input
             {
                 float4 pos : POSITION;
                 half2 uv : TEXCOORD0;
             };

              struct output
             {
                 float4 pos : SV_POSITION;
                 half2 uv : TEXCOORD0;
             };


              output vert(input i)
             {
                 output o;
                 o.pos = UnityObjectToClipPos(i.pos);
                 o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, i.uv);
                 // why do we need this? cause sometimes the image I get is flipped. see: http://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                 #if UNITY_UV_STARTS_AT_TOP
                 if (_MainTex_TexelSize.y < 0)
                         o.uv.y = 1 - o.uv.y;
                 #endif

                  return o;
             }

              fixed4 frag(output o) : COLOR
             {
                 //depth01 = pow(LinearEyeDepth(depth01), _DepthLevel);
                 float depth01 = (Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv))));
                //  (LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv)))) / (_ProjectionParams.z - _ProjectionParams.y);
                 return fixed4(depth01, depth01, depth01, depth01);
             }

              ENDCG
         }
     } 
 } 
 