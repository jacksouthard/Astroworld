Shader "Custom/DistantPlanet"
{
    Properties
    {
		_FalloffStart("Falloff Start Percent", Range(0,1)) = 0.5
	}
    SubShader
    {
		Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
        LOD 100

		Blend SrcAlpha OneMinusSrcAlpha // Additive blending.
		ColorMask RGBA // This is changed from "RGB" to "RGBA"
		Cull Off Lighting Off ZWrite Off
		//ZWrite Off // Depth test off.

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
            };

			float _FalloffStart;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				//return fixed4(i.uv.xy, 0, 0);
				float p = length(i.uv - float2(0.5, 0.5)) * 2;// / 1.41421356;
				clip(1 - p);
				float a = 1;
				if (p > _FalloffStart) {
					// do falloff
					a = 1 - (p - _FalloffStart) / (1 - _FalloffStart);
					const int toonCuts = 3;
					a = floor(a * toonCuts) / toonCuts;
					a = pow(a, 3);
				}
                return fixed4(i.color.rgb, i.color.a * a);
            }
            ENDCG
        }
    }
}
