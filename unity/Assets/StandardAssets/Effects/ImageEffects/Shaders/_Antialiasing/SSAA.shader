
Shader "Hidden/SSAA" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

// very simple & fast AA by Emmanuel Julien

SubShader {
	Pass {
	
	ZTest Always Cull Off ZWrite Off

	CGPROGRAM

	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

	uniform sampler2D _MainTex;
	uniform float4 _MainTex_TexelSize;
	half4 _MainTex_ST;

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv[5] : TEXCOORD0;
	};

	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		
		float2 uv = v.texcoord.xy;
	    
	    float w = 1.75;
				
		float2 up = float2(0.0, _MainTex_TexelSize.y) * w;
		float2 right = float2(_MainTex_TexelSize.x, 0.0) * w;	
			
		o.uv[0].xy = uv - up;
		o.uv[1].xy = uv - right;
		o.uv[2].xy = uv + right;
		o.uv[3].xy = uv + up;

		o.uv[4].xy = uv;
		
		return o;
	}

	half4 frag (v2f i) : SV_Target
	{		 	 
		half4 outColor;
		
	    float t = Luminance( tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[0], _MainTex_ST)).xyz );
		float l = Luminance( tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[1], _MainTex_ST)).xyz);
		float r = Luminance( tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[2], _MainTex_ST)).xyz);
		float b = Luminance( tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[3], _MainTex_ST)).xyz);
	 
	    half2 n = half2( -( t - b ), r - l );
	    float nl = length( n );
	 
	    if ( nl < (1.0 / 16.0) )
			outColor = tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[4], _MainTex_ST) );
	    else {
			n *= _MainTex_TexelSize.xy / nl;
	 
			half4 o = tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[4], _MainTex_ST));
			half4 t0 = tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[4] + n * 0.5, _MainTex_ST)) * 0.9;
			half4 t1 = tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[4] - n * 0.5, _MainTex_ST)) * 0.9;
			half4 t2 = tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[4] + n, _MainTex_ST)) * 0.75;
			half4 t3 = tex2D( _MainTex, UnityStereoScreenSpaceUVAdjust(i.uv[4] - n, _MainTex_ST)) * 0.75;
	 
			outColor = (o + t0 + t1 + t2 + t3) / 4.3;
	    }
	    
	    return outColor;
	}
	
	ENDCG
	}
}

Fallback off

}
