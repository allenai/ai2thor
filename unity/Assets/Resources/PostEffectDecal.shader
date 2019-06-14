Shader "Custom/PostEffectDecal"
{
    Properties 
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [IntRange] _StencilRef ("Stencil Reference Value", Range(0,1)) = 0
    }
    SubShader
    {

        Tags {
                "RenderType"="Opaque"
                "Queue"="Transparent+2"
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
                float4 uv: TEXCOORD3;
            };
           
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.sreenPos = ComputeScreenPos(o.pos);
  
                o.rayToCamera = mul(UNITY_MATRIX_MV, float4(v.vertex.xyz, 1)).xyz * float3(-1, -1, 1);

                o.uv = v.texcoord;
                return o;
            }

            // Unity Comments this and replaces with unity_CameraToWorld
            CBUFFER_START(UnityPerCamera2)
			// float4x4 _CameraToWorld;
			CBUFFER_END
           
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            half _Emissiveness;

 
            fixed4 frag (v2f i): SV_Target
            {
                // float2 uv = i.sreenPos.xy / i.sreenPos.w;
                i.rayToCamera = i.rayToCamera * (_ProjectionParams.z / i.rayToCamera.z);
               
                float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, i.sreenPos));

                float4 cameraSpacePos = float4(i.rayToCamera * depth, 1.0);
                float3 worldPos = mul(unity_CameraToWorld, cameraSpacePos);
                float3 objectPos = mul(unity_WorldToObject, float4(worldPos, 1));

                clip(float3(0.5, 0.5, 0.5) - abs(objectPos.xyz));

                fixed4 color = tex2D(_MainTex, float2(-(objectPos.x + 0.5), objectPos.y + 0.5));

                // fixed4 color  = tex2D(_MainTex, i.uv.xy);
                return color;
            }

            

            ENDCG
        }
    }
}