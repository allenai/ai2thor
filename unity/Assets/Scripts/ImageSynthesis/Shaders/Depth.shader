// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Depth" {
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
                 // depth01 = pow(LinearEyeDepth(depth01), _DepthLevel);
                 // https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
                 // far - near plane
                 float multiplier = _ProjectionParams.z - _ProjectionParams.y;
                 float depth01 = Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv))) * multiplier;
                 //// This converts the float to an int bitwise (not a cast)
                 //// https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/asint
                 uint depthUint = asint(depth01);

                 float f1 = float(depthUint & 255) / 255.0;
                 float f2 = float((depthUint >> 8) & 255) / 255.0;
                 float f3 = float((depthUint >> 16) & 255)  / 255.0;
                 float f4 = float((depthUint >> 24) & 255) / 255.0;

                 return fixed4(f1, f2, f3, f4);
             }
             
             ENDCG
         }
     } 
 }
