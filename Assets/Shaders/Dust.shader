Shader "Custom/Dust"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _MaxAlpha("Max alpha", Range(0,1)) = 1
        _Visibility("Visibility", Range(0,1)) = 1
        _CircluarCutoffHigh("Circular Cutoff High", Range(0,1)) = 0.8
        [NoScaleOffset]_Noise("Noise", 2D) = "white" {}
        _Scale("Noise Scale", Range(0,3)) = 1
        _Cutoff("Cutoff", Range(0,1)) = 0.5
        [NoScaleOffset]_Mask("Mask", 2D) = "white" {}
        _MaskScale("Mask Scale", Range(0,3)) = 1
        _MaskCutoffLow("Mask Cutoff Low", Range(0,1)) = 0.3
        _MaskCutoffHigh("Mask Cutoff High", Range(0,1)) = 0.6
        [Toggle(MASK)] _DoMask("Do Mask", Float) = 0
        [Toggle(TOON)] _DoToon("Do Toon", Float) = 0
        _RandomOffsets("Random Offsets", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha // Additive blending.

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_local _ MASK
            #pragma multi_compile_local _ TOON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            half4 _Color;
            
            sampler2D _Noise;
            
            half _MaxAlpha;
            half _Visibility;
            half _Cutoff;
            half _Scale;
            
            sampler2D _Mask;
            half _MaskScale;
            half _MaskCutoffLow;
            half _MaskCutoffHigh;
            
            half _CircluarCutoffHigh;
            
            fixed4 _RandomOffsets;
            
            float toonRamp(float val) {
                const float toonCuts = 7;
                return floor(val * toonCuts) / toonCuts;
            }
            float calcMask(float2 uv, float2 offset) {
                float m = 1 - tex2D(_Mask, uv * _MaskScale + offset).a;
                if (m < _MaskCutoffLow) {
                    m = 0;
                }
                else if (m > _MaskCutoffHigh) {
                    m = 1;
                }
                else {
                    m = (m - _MaskCutoffLow) / (_MaskCutoffHigh - _MaskCutoffLow);
                }
                return m;
            }
         
            
            fixed4 frag (v2f i) : SV_Target
            {            
                float p = length(i.uv - float2(0.5, 0.5)) * 2; // ratio to outside edge
                clip (1 - p);
                
                float a = tex2D(_Noise, i.uv * _Scale + _RandomOffsets.xy).a;
                a = (a - _Cutoff) / (1 - _Cutoff);
                
                float circA = 1;
                if (p > _CircluarCutoffHigh) {
                    circA = 1 - (p - _CircluarCutoffHigh) / (1 - _CircluarCutoffHigh);
                }
                
                a *= circA;
                
                #ifdef MASK
                a *= calcMask(i.uv, _RandomOffsets.zw);
                #endif
                                
                #ifdef TOON
                a = toonRamp(a);
                #endif
                
                //clip (a - 0.1);
                
                //a *= circA;
                
                return fixed4(_Color.rgb, a * _Visibility * _MaxAlpha);
            }
            ENDCG
        }
    }
}
