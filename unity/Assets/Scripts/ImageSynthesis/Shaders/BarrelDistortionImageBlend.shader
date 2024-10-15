// latest

Shader "Custom/BarrelDistortion" {
     Properties
     {
         [MainTexture] _MainTex ("Base (RGB)", 2D) = "white" {}

         _LensDistortionStrength ("Lens Distortion Strength", Range (-20.0, 20.0))  = 1.0
        //  _LensDistortionTightness ("Lens Distortion Power", Range (-20.0, 20.0))  = 7.0
         _ZoomPercent ("Zoom Percent", Range (0.0, 5.0))  = 1.0


          _k1 ("K1 polynomial dist coeff", Range (-8.0, 8.0))  = -0.126
          _k2 ("K2 polynomial dist coeff", Range (-8.0, 8.0))  = 0.004
          _k3 ("K3 polynomial dist coeff", Range (-25.0, 25.0))  = 0.0
          _k4 ("K4 polynomial dist coeff", Range (-25.0, 25.0))  = 0.0

         _DistortionIntensityX ("Distort Strength X", Range (0.0, 6.0)) = 1.0
         _DistortionIntensityY ("Distort Strength Y", Range (0.0, 6.0)) = 1.0
          
         
         _OutOfBoundColour ("Outline Color", Color) = (0.0, 0.0, 0.0, 1.0)

         _fov_y ("Vertical Fov", Range (-360.0, 360.0)) = 0.0

         _Virtual_To_Real_Blend ("Blend From Virtual To Real", Range (0.0, 1.0)) = 0.0

         _RealImage ("Real Image To Bleand", 2D) = "white" {}
     }
     SubShader
     {
         Pass
         {
             CGPROGRAM

              #pragma vertex vert
             #pragma fragment frag
             #pragma target 3.0 
             #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform sampler2D _RealImage;
             uniform sampler2D _CameraDepthTexture;
             uniform half4 _MainTex_TexelSize;

             uniform float _LensDistortionStrength;
             uniform float4 _OutOfBoundColour;
            //  uniform float _LensDistortionTightness;

             uniform float _ZoomPercent;

            uniform float _DistortionIntensityX;
            uniform float _DistortionIntensityY;
             
             uniform float _k1;
             uniform float _k2;
             uniform float _k3;
             uniform float _k4;

             uniform float _fov_y;

             uniform float _Virtual_To_Real_Blend;

            //  uniform float4 _ScreenParams;

             
            //  uniform float4 _ScreenParams;
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
                //  float2 clipPos : UNITY_VPOS_TYPE;
             };


              output vert(input i)
             {
                 output o;
                //  o.screenPos = i.pos;
                 o.pos = UnityObjectToClipPos(i.pos);
                //  o.uv = i.uv;
                 o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, i.uv);
                //  o.uv.y = 1 - o.uv.y;
                //  // why do we need this? cause sometimes the image I get is flipped. see: http://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                //  #if UNITY_UV_STARTS_AT_TOP
                //  if (_MainTex_TexelSize.y < 0)
                //          o.uv.y = 1 - o.uv.y;
                //  #endif
                // if (_ProjectionParams.x >= 0)
                //     o.uv.y = 1 - o.uv.y;

                  return o;
             }

             float3x3 cam_intrinsics(float fov_y, float frame_height, float frame_width) {
                float focal_length = 0.5 * frame_height / tan((UNITY_PI / 180.0) *(fov_y / 2));
                float f_x = focal_length;
                float f_y = f_x;

                float c_x = frame_width / 2;
                float c_y = frame_height / 2;
                return float3x3(float3(f_x, 0, c_x), float3(0, f_y, c_y), float3(0, 0, 1));
             }

              fixed4 frag(output o) : COLOR
             {

                // return float4(o.screenPos.x, 0.0, o.screenPos.y, 1.0);
                float effect = _LensDistortionStrength;
                float2 distortionStrengthXY = float2(_DistortionIntensityX, _DistortionIntensityY);
                float zoom_offset = (1.0 - _ZoomPercent) / 2.0;

                float3x3 k = cam_intrinsics(_fov_y, _ScreenParams.y, _ScreenParams.x);

                float2 centered_uv = o.uv - float2(0.5, 0.5);
                centered_uv = o.uv*2.0 -  float2(1.0, 1.0);
                //  centered_uv = o.uv -  float2(0.5, 0.5);

                zoom_offset = (1.0 - _ZoomPercent);
                centered_uv = (o.uv*2.0 * _ZoomPercent - float2(1.0, 1.0)) + float2(zoom_offset, zoom_offset);  
                
                
               
                float uv_dot = dot(centered_uv, centered_uv);
                float r = sqrt(uv_dot);
                // // For Atan based distortion
                float z = sqrt(1.0 - uv_dot * effect);
                // float z = sqrt(1.0 - r * r);
                float atan_distort = atan2(r, z) / UNITY_PI;

                float atan_distortX = atan2(r, sqrt(1.0 - uv_dot * _DistortionIntensityX)) / UNITY_PI;
                float atan_distortY = atan2(r, sqrt(1.0 - uv_dot * _DistortionIntensityY)) / UNITY_PI;
                float2 distort_uv = (centered_uv / r) * atan_distort;//+ float2(0.5, 0.5);

                distort_uv = (centered_uv / r) * float2(atan_distortX, atan_distortY);
               
                //return t;

                const float distortionMagnitude=abs(centered_uv.x*centered_uv.y);

                float smoothDistortionMagnitude = 1.0 + _k1 * pow(r, 2.0) + _k2 * pow(r, 4.0) + _k3 * pow(r,6.0) + _k4 * pow(r,8.0);

                float zoom_percent = 0.2;
                float translate = (1.0 - zoom_percent) / 2.0;
                float2 centered_uv_norm = normalize(centered_uv);

               
                
                // Works with zoom for poly distortion
                float2 uvDistorted = centered_uv * smoothDistortionMagnitude * distortionStrengthXY / 2.0 + float2(0.5, 0.5) ;

                // working for atan distortion
                // float2 uvDistorted = ( centered_uv / r ) * atan_distort + float2(0.5, 0.5);

                // float2 uvDistorted = distort_uv + float2(0.5, 0.5);

                // for xydistortion
                // float2 uvDistorted = distort_uv*2.0 + float2(0.5, 0.5);

            

            fixed4 col = tex2D(_MainTex, uvDistorted);
            // Handle out of bound uv
            if (uvDistorted.x < 0 || uvDistorted.x > 1 || uvDistorted.y < 0 || uvDistorted.y > 1) {
                return _OutOfBoundColour;//uv out of bound so display out of bound color
            } else {
                return lerp(col, tex2D(_RealImage, o.uv), _Virtual_To_Real_Blend);
            }

            // FOR atan radius cutoff
            // float radius = 1.0;
            //     if (uvDistorted.x > radius) {
            //         return _OutOfBoundColour;
            //     }
            //     else {
            //         return lerp(col1, col2, _BlendAmount);
            //     }

            
             }

              ENDCG
         }
     } 
 } 
 