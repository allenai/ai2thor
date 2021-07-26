/**
 * Wireframe shader adapted from:
 * http://codeflow.org/entries/2012/aug/02/easy-wireframe-display-with-barycentric-coordinates/
 */

Shader "Custom/Wireframe"
{
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
	_Color ("Wire Color", Color) = (0,0,0,1)
	_Thickness ("Wire Thickness", Range(0, 1)) = .5
	_Opacity ("Wire Opacity", Range (0, 1)) = .8
  }

  SubShader
  {
    Tags { "RenderType" = "Opaque" }

    ColorMask RGB

    CGPROGRAM
    #pragma surface surf Lambert

    sampler2D _MainTex;
    fixed4 _Color;
    float _Thickness;
    float _Opacity;

    struct Input {
        float4 color : COLOR;
        float2 uv_MainTex;
    };

    float edgeFactor(fixed3 pos)
    {
    	fixed3 d = fwidth(pos);
    	fixed3 a3 = smoothstep( fixed3(0.0,0.0,0.0), d * _Thickness, pos);
    	return min(min(a3.x, a3.y), a3.z);
    }

    void surf (Input IN, inout SurfaceOutput o) {

    	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
        fixed4 c = lerp( _Color, tex, edgeFactor(IN.color) );
        c = lerp(tex, c, _Opacity);

        o.Albedo = c.rgb;
    }
    ENDCG
  }

  Fallback "Diffuse"
}
