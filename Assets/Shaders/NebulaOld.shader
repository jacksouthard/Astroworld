Shader "Custom/NebulaOld"
{
    Properties
    {
		_Color("Tint", Color) = (0, 0, 0, 1)
		_Scale("Scale", Range(0,1)) = 1
		_Density("Density", Range(0,0.2)) = 0.1
		_Falloff("Falloff", Range(3,5)) = 3
		[NoScaleOffset]_Noise("Noise", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            sampler2D _Noise;
            float4 _Noise_ST;

			half4 _Color;
			half _Scale;
			half _Density;
			half _Falloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Noise);
                return o;
            }

			float noise(float2 p) {
				const int steps = 5;
				float scale = pow(2.0, steps);
				float displace = 0.0;
				for (int i = 0; i < steps; i++) {
					displace = tex2D(_Noise, p * scale + displace).a;
					scale *= 0.5;
				}
				return tex2D(_Noise, p + displace).a;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float n = noise(i.uv * _Scale);
				n = pow(n + _Density, _Falloff);

				fixed4 col;
				col.rgb = n;
				col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
