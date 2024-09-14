Shader "Hidden/DepthBW" {
     Properties
     {
         [MainTexture] _MainTex ("Base (RGB)", 2D) = "white" {}
         _DepthLevel ("Depth Level", Range(1, 3)) = 1
         _LensDistortionStrength ("Lens Distortion Strength", Range (-20.0, 20.0))  = 1.0
         _LensDistortionTightness ("Lens Distortion Power", Range (-20.0, 20.0))  = 7.0
         _ZoomPercent ("Zoom Percent", Range (0.0, 5.0))  = 1.0

         _DistortionIntensityX ("Distort X", Range (0.0, 6.0)) = 1.0
         _DistortionIntensityY ("Distort Y", Range (0.0, 6.0)) = 1.0


          _k1 ("K1 polynomial dist coeff", Range (-8.0, 8.0))  = -0.126
          _k2 ("K2 polynomial dist coeff", Range (-8.0, 8.0))  = 0.004
          _k3 ("K3 polynomial dist coeff", Range (-8.0, 8.0))  = 0.0
          _k4 ("K4 polynomial dist coeff", Range (-8.0, 8.0))  = 0.0
          _k5 ("K5 polynomial dist coeff", Range (-8.0, 8.0))  = 0.0
          _k6 ("K6 polynomial dist coeff", Range (-8.0, 8.0))  = 0.0
          
         
         _OutOfBoundColour ("Outline Color", Color) = (0.0, 0.0, 0.0, 1.0)
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

             uniform float _LensDistortionStrength;
             uniform float4 _OutOfBoundColour;
             uniform float _LensDistortionTightness;

             uniform float _ZoomPercent;

            uniform float _DistortionIntensityX;
            uniform float _DistortionIntensityY;
             
             uniform float _k1;
             uniform float _k2;
             uniform float _k3;
             uniform float _k4;
             uniform float _k5;
             uniform float _k6;

             
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
             };


              output vert(input i)
             {
                 output o;
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

              fixed4 frag(output o) : COLOR
             {
                 // depth01 = pow(LinearEyeDepth(depth01), _DepthLevel);
                 float depth01 = (Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv))));
                //  (LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv)))) / (_ProjectionParams.z - _ProjectionParams.y);
                //  return fixed4(depth01, depth01, depth01, depth01);
                // return fixed4(o.uv.x, 0, o.uv.y, 1.0);
                // _ScreenParams.x 

                // float zoom_percent_0 = 1.0;
                // float translate_0 = (1.0 - zoom_percent_0) / 2.0;

                // screen centered frag coordinate
                float effect = _LensDistortionStrength;
                float zoom_offset = (1.0 - _ZoomPercent) / 2.0;

                // float2 offset = float2(0.5, 0.5);
                // float2 centered_uv = o.uv*1.0 * zoom_percent_0 - float2(0.5, 0.5) + translate_0;
                // float2 centered_uv = (o.uv*2.0 - float2(1.0, 1.0));  
                // float2 centered_uv = (o.uv*2.0 * _ZoomPercent - float2(1.0, 1.0)) + float2(zoom_offset, zoom_offset);  
                float2 centered_uv = o.uv - float2(0.5, 0.5);
                centered_uv = o.uv*2.0 -  float2(1.0, 1.0);

                zoom_offset = (1.0 - _ZoomPercent);
                centered_uv = (o.uv*2.0 * _ZoomPercent - float2(1.0, 1.0)) + float2(zoom_offset, zoom_offset);  
                
                // float2 centered_uv = (o.uv * _ZoomPercent - float2(0.5, 0.5)) + float2(zoom_offset, zoom_offset);

                // float2 centered_uv = (o.uv - float2(0.5, 0.5));// + float2(zoom_offset, zoom_offset);
                
                // centered_uv = centered_uv * _ZoomPercent + float2(zoom_offset, zoom_offset);
                // float d=length(centered_uv);
                // float z = sqrt(1.0 - d * d);

                float uv_dot = dot(centered_uv, centered_uv);
                float r = sqrt(uv_dot);
                float z = sqrt(1.0 - uv_dot * effect);
                // float z = sqrt(1.0 - r * r);
                float atan_distor = atan2(r, z) / 3.14159;

                float atan_distortX = atan2(r, sqrt(1.0 - uv_dot * _DistortionIntensityX)) / 3.14159;
                float atan_distortY = atan2(r, sqrt(1.0 - uv_dot * _DistortionIntensityY)) / 3.14159;


                // float theta = atan(r);
                // float theta_d = theta * (1 + _k1 * pow(theta, 2.0) + _k2 * pow(theta, 4.0) + _k3 * pow(theta, 6.0) + _k4 * pow(theta, 8.0));
                // float focal = 1.0;
                // float r_d = focal * theta_d;    

                // atan_distor = r_d / r;


                // atan_distor = 
                // float phi = atan(centered_uv.y / centered_uv.x);
                
                // float2 distort_uv = float2(atan_distor*cos(phi)+.5,atan_distor*sin(phi)+.5);

                // // float2 centered_uv = o.uv - offset;
                // float4 dot_coord = dot(centered_uv, centered_uv);
                // // radius
                // float r = sqrt(dot_coord);

                // float z = sqrt(1.0 - dot_coord);
                // float pi = 3.14159;

                // float distort = atan(r / z) / pi;


                // float phi = atan(centered_uv.y / centered_uv.x);

                // // float2 distort_uv = float2(atan_distor * cos(phi), atan_distor * sin(phi));
                // float distort_uv = ( centered_uv / r ) * distort + offset;
                // float _LensDistortionTightness = 7.0;

                float2 distort_uv = (centered_uv / r) * atan_distor + float2(0.5, 0.5);
                float4 t = tex2D (_MainTex, distort_uv);
                //return t;

                const float distortionMagnitude=abs(centered_uv.x*centered_uv.y);
                // float smoothDistortionMagnitude = pow(r, _LensDistortionTightness);//use exponential function
                float smoothDistortionMagnitude = pow(distortionMagnitude, _LensDistortionTightness);
                smoothDistortionMagnitude = 1.0 + _k1 * pow(r, 2.0) + _k2 * pow(r, 4.0) + _k3 * pow(r,6.0) + _k4 * pow(r,8.0);

                // smoothDistortionMagnitude = atan(r) / 3.14159;
                // return float4(smoothDistortionMagnitude, smoothDistortionMagnitude, smoothDistortionMagnitude, 1.0);

                // smoothDistortionMagnitude = 1 + _k1 * pow(distortionMagnitude, 2.0) + _k2 * pow(distortionMagnitude, 4.0) + _k3 * pow(distortionMagnitude,6.0) + _k4 * pow(distortionMagnitude,8.0);
                // smoothDistortionMagnitude = 1 + _k1 * pow(r, 2.0) + _k2 * pow(r, 4.0) + _k3 * pow(r,6.0) + _k4 * pow(r,8.0);
                //const float smoothDistortionMagnitude=1-sqrt(1-pow(distortionMagnitude,_LensDistortionTightness));//use circular function
                //const float smoothDistortionMagnitude=pow(sin(UNITY_HALF_PI*distortionMagnitude),_LensDistortionTightness);// use sinusoidal 
                // r = distortionMagnitude;
                // float smoothDistortionMagnitude = atan(length(centered_uv));
                // return smoothDistortionMagnitude;

                // Atan distortion
                // smoothDistortionMagnitude = atan_distor;

                // r = distortionMagnitude;

                // smoothDistortionMagnitude = 1 + _k1 * pow(r, 2.0) + _k2 * pow(r, 4.0) + _k3 * pow(r,6.0) + _k4 * pow(r,8.0);

                // float2 uvDistorted = o.uv + centered_uv * smoothDistortionMagnitude * _LensDistortionStrength ; //vector of distortion and add it to original uv
                
                float zoom_percent = 0.2;
                float translate = (1.0 - zoom_percent) / 2.0;
                float2 centered_uv_norm = normalize(centered_uv);
                // float2 uvDistorted = centered_uv_norm * smoothDistortionMagnitude * _LensDistortionStrength;
                // uvDistorted = 0.5 * (uvDistorted + float2(1.0, 1.0));
                
                // float2 uvDistorted = (o.uv + centered_uv * smoothDistortionMagnitude * _LensDistortionStrength) ; //vector of distortion and add it to original uv
                // float2 uvDistorted = centered_uv * smoothDistortionMagnitude * _LensDistortionStrength / 2.0 + float2(0.5, 0.5) ;
                
                // Works with zoom for poly distortion
                float2 uvDistorted = centered_uv * smoothDistortionMagnitude * _LensDistortionStrength / 2.0 + float2(0.5, 0.5) ;

                // working for atan distortion
                // float2 uvDistorted = ( centered_uv / r ) * smoothDistortionMagnitude * _LensDistortionStrength + float2(0.5, 0.5);

                // float2 distorted = ( centered_uv / r ) * 
                // float2 uvDistorted = ( centered_uv / r ) * float2(atan_distortX, atan_distortY)  + float2(0.5, 0.5);

                //  float2 uvDistorted = ( centered_uv / r ) * smoothDistortionMagnitude * _LensDistortionStrength + float2(0.5, 0.5);
                // float2 uvDistorted = ( centered_uv / r ) * smoothDistortionMagnitude * _LensDistortionStrength + float2(0.5, 0.5);

                // float2 uvDistorted = (centered_uv / r) * smoothDistortionMagnitude * _LensDistortionStrength + float2(0.5, 0.5);
                //uvDistorted = distort_uv;
                float radius = 1.0;
                // if (uvDistorted.x > radius) {
                //     return _OutOfBoundColour;
                // }
                // else {
                //     return tex2D(_MainTex, uvDistorted);
                // }
                // float pl = length(float2(o.pos.x, o.pos.y));
                // return float4(o.pos.x/pl, 0.0, o.pos.y/pl, 1.0);

            // uvDistorted = uvDistorted * 0.3 + float2(0.37, 0.37);
            // uvDistorted = uvDistorted * _ZoomPercent + float2(zoom_offset, zoom_offset);
            
            // uvDistorted = uvDistorted * zoom_percent + float2(translate, translate);
            // return float4(uvDistorted.x, 0.0, uvDistorted.y, 1.0);
  //Handle out of bound uv
            zoom_percent = _ZoomPercent;
            translate = (1.0 - zoom_percent);
            // centered_uv = (centered_uv / 2.0 + float2(0.5, 0.5)) * _ZoomPercent + translate;

            centered_uv = o.uv*2.0 *_ZoomPercent  -  float2(1.0, 1.0) + translate;

            float2 no_zoom_centered_uv = o.uv*2.0 - float2(1.0, 1.0);

            float no_zoom_uv_dot = dot(no_zoom_centered_uv, no_zoom_centered_uv);
            float no_zoom_uv_len = sqrt(no_zoom_uv_dot);

            float centered_uv_len = length(centered_uv);

            r = centered_uv_len;

            float distort = _k1 * pow(r, 2.0) + _k2 * pow(r, 4.0) + _k3 * pow(r,6.0) + _k4 * pow(r,8.0);

            float2 distorted_uv = ((centered_uv / r)) * distort;
            
            // distorted_uv = o.uv + distorted_uv;
            // float2 distorted_uv =  (no_zoom_centered_uv/centered_uv_len) * distort;
            centered_uv = (distorted_uv / 2.0 + float2(0.5, 0.5));
            // centered_uv = distorted_uv;// + float2(0.5, 0.5);
            
            // centered_uv = (centered_uv / 2.0 + float2(0.5, 0.5));
  
            // if (centered_uv.x < 0 || centered_uv.x > 1 || centered_uv.y < 0 || centered_uv.y > 1) {
            //     return _OutOfBoundColour;//uv out of bound so display out of bound color
            // } else {
            
            // return tex2D(_MainTex, centered_uv);
            // }
            // return fixed4(centered_uv.x, 0, centered_uv.y, 1.0f);
            if (uvDistorted.x < 0 || uvDistorted.x > 1 || uvDistorted.y < 0 || uvDistorted.y > 1) {
                return _OutOfBoundColour;//uv out of bound so display out of bound color
            } else {
                // uvDistorted = uvDistorted * zoom_percent + translate;
                return tex2D(_MainTex, uvDistorted);
            }
            // return tex2D(_MainTex, uvDistorted);
            

                


                // return fixed4(o.uv.x, 0, o.uv.y, t.z);
                // return t;
                // float2 tr = (centered_uv / r) * atan_distor + float2(0.5, 0.5);
                // // float2 tr = float2(cos(phi), sin(phi));
                // return float4(tr.x, 0.0, tr.y, 1.0);
             }

              ENDCG
         }
     } 
 } 
 