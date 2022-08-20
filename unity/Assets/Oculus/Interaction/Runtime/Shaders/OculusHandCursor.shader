/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/OculusHandCursor"
{
    Properties
    {
        _OutlineWidth("OutlineWidth", Range( 0 , 0.4)) = 0.03
        _CenterSize("Center Size", Range( 0 , 0.5)) = 0.15
        _Color("Inner Color", Color) = (0,0,0,0)
        _OutlineColor("OutlineColor", Color) = (0,0.4410214,1,0)
        _Alpha("Alpha", Range( 0 , 1)) = 0
        _RadialGradientIntensity("RadialGradientIntensity", Range( 0 , 1)) = 0
        _RadialGradientScale("RadialGradientScale", Range( 0 , 1)) = 1
        _RadialGradientBackgroundOpacity("RadialGradientBackgroundOpacity", Range( 0 , 1)) = 0.1
        _RadialGradientOpacity("RadialGradientOpacity", Range( 0 , 1)) = 0.8550259
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+10" "IgnoreProjector" = "True"  }
        Cull Off
        ZTest LEqual
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Offset -5, -5
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

            uniform float _RadialGradientScale;
            uniform float _RadialGradientOpacity;
            uniform float _RadialGradientIntensity;
            uniform float _RadialGradientBackgroundOpacity;
            uniform float _OutlineWidth;
            uniform float4 _Color;
            uniform float4 _OutlineColor;
            uniform float _CenterSize;
            uniform float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv_texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_texcoord = v.uv_texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float RadialGradientScaleRaw149 = _RadialGradientScale;
                float RadialGradientScale94 = (0.16 + (_RadialGradientScale - 0.0) * (0.45 - 0.16) / (1.0 - 0.0));
                float temp_output_1_0_g49 = ( 1.0 - ( ( distance( i.uv_texcoord , float2( 0.5,0.5 ) ) * 1.0 ) / RadialGradientScale94 ) );
                float RadialGradientIntensity96 = (5.0 + (_RadialGradientIntensity - 0.0) * (1.5 - 5.0) / (1.0 - 0.0));
                float ifLocalVar12_g49 = 0;
                if( temp_output_1_0_g49 <= 0.0 )
                    ifLocalVar12_g49 = 1.0;
                else
                    ifLocalVar12_g49 = ( 1.0 / pow( 2.718282 , ( temp_output_1_0_g49 * RadialGradientIntensity96 ) ) );
                float temp_output_1_0_g47 = ( 1.0 - ( ( distance( i.uv_texcoord , float2( 0.5,0.5 ) ) * 1.0 ) / RadialGradientScale94 ) );
                float RadialDensity131 = 70.0;
                float ifLocalVar12_g47 = 0;
                if( temp_output_1_0_g47 <= 0.0 )
                    ifLocalVar12_g47 = 1.0;
                else
                    ifLocalVar12_g47 = ( 1.0 / pow( 2.718282 , ( temp_output_1_0_g47 * RadialDensity131 ) ) );
                float temp_output_75_0 = ( 1.0 - ifLocalVar12_g47 );
                float RadialGradient102 = saturate( ( ( _RadialGradientOpacity * ( ( 1.0 - ( 1.0 - ifLocalVar12_g49 ) ) - ( 1.0 - temp_output_75_0 ) ) ) + ( temp_output_75_0 * _RadialGradientBackgroundOpacity ) ) );
                float temp_output_1_0_g77 = ( 1.0 - ( ( distance( i.uv_texcoord , float2( 0.5,0.5 ) ) * 1.0 ) / ( RadialGradientScale94 + _OutlineWidth ) ) );
                float ifLocalVar12_g77 = 0;
                if( temp_output_1_0_g77 <= 0.0 )
                    ifLocalVar12_g77 = 1.0;
                else
                    ifLocalVar12_g77 = ( 1.0 / pow( 2.718282 , ( temp_output_1_0_g77 * 20.0 ) ) );
                float4 RadialGradientWithOutline147 = ( RadialGradient102 + ( ( ( 1.0 - ifLocalVar12_g77 ) - temp_output_75_0 ) * _OutlineColor ) );
                float temp_output_1_0_g81 = ( 1.0 - ( ( distance( i.uv_texcoord , float2( 0.5,0.5 ) ) * 1.0 ) / _CenterSize ) );
                float RadialDensityOutline189 = 20.0;
                float ifLocalVar12_g81 = 0;
                if( temp_output_1_0_g81 <= 0.0 )
                    ifLocalVar12_g81 = 1.0;
                else
                    ifLocalVar12_g81 = ( 1.0 / pow( 2.718282 , ( temp_output_1_0_g81 * RadialDensityOutline189 ) ) );
                float temp_output_1_0_g79 = ( 1.0 - ( ( distance( i.uv_texcoord , float2( 0.5,0.5 ) ) * 1.0 ) / ( _CenterSize + 0.06 ) ) );
                float ifLocalVar12_g79 = 0;
                if( temp_output_1_0_g79 <= 0.0 )
                    ifLocalVar12_g79 = 1.0;
                else
                    ifLocalVar12_g79 = ( 1.0 / pow( 2.718282 , ( temp_output_1_0_g79 * RadialDensityOutline189 ) ) );
                float4 OutlineColor183 = _OutlineColor;
                float4 CenterDot143 = ( ( 1.0 - ifLocalVar12_g81 ) + ( ( 1.0 - ifLocalVar12_g79 ) * OutlineColor183 ) );
                float4 ifLocalVar29 = 0;
                if( RadialGradientScaleRaw149 <= 0.1 )
                    ifLocalVar29 = CenterDot143;
                else
                    ifLocalVar29 = RadialGradientWithOutline147;
                float4 Emission151 = ifLocalVar29 * _Color;

                float Opacity152 = ((ifLocalVar29).a * _Alpha);
                if (Opacity152 < 0.01)
                    discard;
                return float4 (Emission151.rgb, Opacity152);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
