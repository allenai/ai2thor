Shader "Unlit/SpecialFX/TransparentLiquidMix"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _MixTint ("MixTint", Color) = (1,1,1,1)
        
        _FillAmount ("Fill Amount", Range(-10,10)) = 0.0
        _MixRim ("Mix Line Width", Range(0,1.0)) = 0.0
        [HideInInspector] _WobbleX ("WobbleX", Range(-1,1)) = 0.0
        [HideInInspector] _WobbleZ ("WobbleZ", Range(-1,1)) = 0.0
        _TopColor ("Top Color", Color) = (1,1,1,1)
            
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0,10)) = 0.0
        
    }
 
    SubShader
    {
        Tags {
          "Queue"="Transparent"
        }
 
        Pass
        {
         Cull Off
         AlphaToMask On
         Blend SrcAlpha OneMinusSrcAlpha
 
         CGPROGRAM
 
 
         #pragma vertex vert
         #pragma fragment frag
           
         #include "UnityCG.cginc"
 
         struct appdata
         {
           float4 vertex : POSITION;
           float3 normal : NORMAL; 
         };
 
         struct v2f
         {
            float4 vertex : SV_POSITION;
            float3 viewDir : COLOR;
            float3 normal : COLOR2;    
            float fillEdge : TEXCOORD2;
         };
 
         float4 _MainTex_ST;
         float _FillAmount, _WobbleX, _WobbleZ;
         float4 _TopColor, _RimColor, _Tint, _MixTint;
         float _MixRim, _RimPower;
           
         float4 RotateAroundY (float4 vertex, float angleRadians)
         {
            float sina, cosa;
            sincos(angleRadians, sina, cosa);
            float2x2 m = float2x2(cosa, sina, -sina, cosa);
            return float4(vertex.yz , mul(m, vertex.xz)).xzyw ;            
         }
     
 
         v2f vert (appdata v)
         {
            v2f o;
 
            float4 vert = v.vertex;
            //vert.y += sin(_Time.y * 0.001);

            o.vertex = UnityObjectToClipPos(vert);
            // get world position of the vertex
 
            float3 worldPos = mul (unity_ObjectToWorld, vert.xyz); 

            // float3 fillAmountWorld = mul (unity_ObjectToWorld, float3(0, _FillAmount, 0)); 

            // rotate it around XY
            float3 worldPosX= RotateAroundY(float4(worldPos,0), 2 * UNITY_PI);
            // rotate around XZ
            float3 worldPosZ = float3 (worldPosX.y, worldPosX.z, worldPosX.x);     
            // combine rotations with worldPos, based on sine wave from script
            float3 worldPosAdjusted = worldPos + (worldPosX  * _WobbleX)+ (worldPosZ * _WobbleZ);
            //worldPosAdjusted.y += sin(_Time.y + worldPosAdjusted.z * 2000)  * 0.009;
 
            // how high up the liquid is
            // o.fillEdge =  worldPosAdjusted.y + fillAmountWorld.y;

            o.fillEdge =  worldPosAdjusted.y + _FillAmount;
 
            o.viewDir = normalize(ObjSpaceViewDir(vert));
            o.normal = v.normal;
            return o;
         }
           
         fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
         {
           // sample the texture
           fixed4 col = _Tint;
           
           // rim light
           float dotProduct = 1 - pow(dot(i.normal, i.viewDir), _RimPower);
           float4 RimResult = smoothstep(0.5, 1.0, dotProduct);
           RimResult *= _RimColor;
 
           // foam edge
           //float4 foam = ( step(i.fillEdge, 0.5) - step(i.fillEdge, (0.5 - _Rim)));

           float4 diff = _Tint - _MixTint;
           float disctinct = dot(diff, diff); 
           float disctinctCutoff = step(0.001, disctinct);

           float4 mix = (step(i.fillEdge, 0.5) - smoothstep(i.fillEdge, i.fillEdge + _MixRim, 0.5)) * disctinctCutoff;
           float4 mixColored = mix * (_MixTint * 0.9);
           // rest of the liquid

           float4 result = step(i.fillEdge, 0.5) - mix;
           float4 resultColored = result * col;
           
           // Main color and mix color
           float4 finalResult = resultColored + mixColored;  

           //finalResult = (resultColored + mixColored ) * 0.5;  

           // Multiplicative mixing
           // finalResult = float4((resultColored.rgb*resultColored.a+mixColored.rgb*mixColored.a),resultColored.a+mixColored.a);           
           finalResult.rgb += RimResult;
 
           // color of backfaces/ top
           float4 topColor = _TopColor * (mix + result) + RimResult;
           //VFACE returns positive for front facing, negative for backfacing

           clip(finalResult.a - 0.0001);

           return facing > 0 ? finalResult: topColor; 
               
         }
         ENDCG
        }
 
    }
}