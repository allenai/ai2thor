// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

  Shader "Hidden/World" {

	  Properties {
			_Opacity ("", Float) = 1
	  }

      SubShader {
          Tags { "Queue"="Transparent" "Render"="Transparent" "IgnoreProjector"="True"}

          //ZWrite Off
          Blend SrcAlpha OneMinusSrcAlpha
  
          Pass{
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
         #pragma fragmentoption ARB_precision_hint_fastest 
         #include "UnityCG.cginc"
         sampler2D _MainTex;
      	 float _Opacity;
     
         struct v2f 
         {
             float4 pos  : POSITION;
             float2 uv   : TEXCOORD0;
             float3 wpos : TEXCOORD1;
         };

         
         v2f vert( appdata_img v )
         {
             v2f o;
             o.pos = UnityObjectToClipPos(v.vertex);
             float3 worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
             o.wpos = worldPos;
             o.uv =  v.texcoord.xy;
             return o;
         }
         
         fixed4 frag (v2f i) : COLOR
         {
         	 //i.wpos.x = (i.wpos.x - _Shift.x) * _Scale.x;
         	 //i.wpos.y = (i.wpos.y - _Shift.y);
         	 //i.wpos.z = (i.wpos.z - _Shift.z);

         	 i.wpos = (i.wpos + 10) / 26.0f;
         	 i.wpos.x = max(0, min(1, i.wpos.x));
         	 i.wpos.y = max(0, min(1, i.wpos.y));
         	 i.wpos.z = max(0, min(1, i.wpos.z));
             return fixed4(i.wpos.x, i.wpos.y, i.wpos.z, _Opacity);
         }
         ENDCG
     }
   } 
}