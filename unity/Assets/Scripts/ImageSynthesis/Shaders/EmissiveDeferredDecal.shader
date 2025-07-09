// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable

Shader "Custom/EmissiveDeferredDecal" {
    Properties 
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [IntRange] _StencilRef ("Stencil Reference Value", Range(0,1)) = 0
        _Emissiveness ("Emissiveness", Range(0,1)) = 0.0
        _Specular ("Specular", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags {
                "RenderType"="Opaque"
				"LightMode" = "Deferred"
			}

        Stencil {
            Ref [_StencilRef]
            ReadMask 1
            Comp Equal
            Pass keep
        }
       
        Pass
        {
            ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct v2f
            {
                float4 pos : SV_POSITION;
               
                float4 sreenPos : TEXCOORD1;
                float3 rayToCamera : TEXCOORD2;
            };
           
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.sreenPos = ComputeScreenPos(o.pos);
  
                o.rayToCamera = mul(UNITY_MATRIX_MV, float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);


                return o;
            }

            CBUFFER_START(UnityPerCamera2)
			// float4x4 _CameraToWorld;
			CBUFFER_END
           
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            half _Emissiveness;
            half _Specular;
 
            void frag (v2f i, out half4 outDiffuse : COLOR0, out half4 outSpecular : COLOR1, out half4 outEmission : COLOR2) //: SV_Target
            {
                i.rayToCamera = i.rayToCamera * (_ProjectionParams.z / i.rayToCamera.z);
               
                // float2 uv = i.sreenPos.xy / i.sreenPos.w;
                // float depth = Linear01Depth(tex2(_CameraDepthTexture, uv));

                // Same as above tex2Dproj does the division by w and takes a vec4
                float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, i.sreenPos));
                

                float4 cameraSpacePos = float4(i.rayToCamera * depth, 1.0);
                float3 worldPos = mul(unity_CameraToWorld, cameraSpacePos);
                float3 objectPos = mul(unity_WorldToObject, float4(worldPos, 1));

                // Discard fragment if it is not inside cube in object space
                clip(float3(0.5, 0.5, 0.5) - abs(objectPos.xyz));

                fixed4 color = tex2D(_MainTex, float2(-(objectPos.x + 0.5), objectPos.y + 0.5));
                
                outDiffuse = color;

                // TODO: Fine tune specular and emsission writes into GBuffer
                // outSpecular = half4(174.0/255, 176.0/255, 180.0/255, 1);
                // outSpecular = half4(color.a*0.72, color.a*0.72, color.a*0.72, color.a);
                // outEmission = half4(0.72, 0.72, 0.72, color.a);
                // outSpecular = half4(color.a*0.72, color.a*0.72, color.a*0.72, color.a);
                // outSpecular = half4(color.a * _Specular , color.a * _Specular, color.a * _Specular, color.a);
                outSpecular = color * _Specular;
                outEmission = color * _Emissiveness;

            }

            ENDCG
        }
    }
}