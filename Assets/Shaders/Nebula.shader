Shader "Custom/Nebula"
{
	Properties
	{
		_Color("Tint", Color) = (0, 0, 0, 1)
		_Scale("Scale", Range(0,3)) = 1
		_DistortionScale("Distortion Scale", Range(0,3)) = 1
		_Distortion("Distortion", Range(0, 1)) = 1
		_Density("Density", Range(0,20)) = 0.5
		_Falloff("Falloff", Range(1,5)) = 3
		[Toggle(TOON)] _DoToon("Do Toon Shading", Float) = 0
		_Cutoff("Cutoff", Range(0,1)) = 0.5
		_Cutoff2("Cutoff2", Range(0,1)) = 0.75
		_MaskCutoffLow("Mask Cutoff Low", Range(0,1)) = 0.3
		_MaskCutoffHigh("Mask Cutoff High", Range(0,1)) = 0.7
		_MaskScale("Mask Scale", Range(0,1)) = 0.5
		[Toggle(MASK2)] _DoMask2("Do Mask 2", Float) = 0
		_Mask2CutoffLow("Mask 2 Cutoff Low", Range(0,1)) = 0.3
		_Mask2CutoffHigh("Mask 2 Cutoff High", Range(0,1)) = 0.7
		_Mask2Scale("Mask 2 Scale", Range(0,1)) = 0.1
		[NoScaleOffset]_Spectrum("Spectrum", 2D) = "white" {}
		[NoScaleOffset]_Noise("Noise", 2D) = "white" {}
		[NoScaleOffset]_Mask2("Mask 2", 2D) = "white" {}
		[NoScaleOffset]_DistortionNoise("Distorition Noise", 2D) = "white" {}
		[NoScaleOffset]_Background("Background", 2D) = "black" {}
		_RandomOffsets("Random Offsets", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
					Cull front

        Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_local _ MASK2
			#pragma multi_compile_local _ TOON

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				half3 worldNormal : TEXCOORD2;
			};

			sampler2D _Noise;
			float4 _Noise_ST;
			sampler2D _DistortionNoise;
			sampler2D _Background;
			sampler2D _Spectrum;
			sampler2D _Mask2;

			half4 _Color;
			half _Scale;
			half _Density;
			half _Falloff;
			half _Cutoff;
			half _Cutoff2;
			half _MaskCutoffLow;
			half _MaskCutoffHigh;
			half _MaskScale;
			half _Mask2CutoffLow;
			half _Mask2CutoffHigh;
			half _Mask2Scale;

			half _DistortionScale;
			half _Distortion;

			fixed4 _RandomOffsets;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Noise);
				o.worldPos = v.vertex;
				o.worldNormal = v.normal;
				return o;
			}

			float noise(float2 p) {
				return tex2D(_Noise, p).a;
			}
			float noise2(float2 p) {
				return tex2D(_DistortionNoise, p).a;
			}
			float mask2Noise(float2 p) {
				return tex2D(_Mask2, p).a;
			}

			float smoothRamp(float val) {
				if (val < _Cutoff) {
					val = 0;
				}
				else {
					val = (val - _Cutoff) / (1 - _Cutoff);
				}
				return val;
			}
			float toonRamp(float val) {
				const float toonCuts = 12;
				val = floor(val * toonCuts) / toonCuts;
				return val;
			}

			float calcMask(float2 uv, float2 offset) {
				float m = 1 - noise(uv * _MaskScale * _Scale + offset);
				if (m < _MaskCutoffLow) {
					m = 0;
				} else if (m > _MaskCutoffHigh) {
					m = 1;
				} else {
					m = (m - _MaskCutoffLow) / (_MaskCutoffHigh - _MaskCutoffLow);
				}
				return m;
			}

			float calcMask2(float2 uv, float2 offset) {
				float m = mask2Noise(uv * _Mask2Scale * _Scale + offset);
				if (m < _Mask2CutoffLow) {
					m = 0;
				}
				else if (m > _Mask2CutoffHigh) {
					m = 1;
				}
				else {
					m = (m - _Mask2CutoffLow) / (_Mask2CutoffHigh - _Mask2CutoffLow);
				}
				m = 1 - pow(1 - m, 4);
				//if (m > 0) m = 1;
				return m;
			}

			float calcLayer(float2 uv, float2 offset) {
				float distortionX = noise2(uv * _DistortionScale * _Scale + offset);
				float distortionY = noise2(uv.yx * _DistortionScale * _Scale * 1.5f + offset);
				float n = noise(uv * _Scale + float2(distortionX, distortionY) * _Distortion + offset);
				//n = 1 - n;
				
				n = smoothRamp(n);
				n *= calcMask(uv, offset);

				#ifdef MASK2
				n *= calcMask2(uv, offset * 1.3 + 0.7);
				#endif
				#ifdef TOON
				n = toonRamp(n);
				#endif
				return n * _Density;
			}

			fixed3 calculateSide(float2 uv) {
				float backgroundAlpha = 0;
				fixed3 col = _Color;

				const int steps = 6;
				float2 offset = 0;
				float2 spec = float2(0, 0);
				for (int i = 0; i < steps; i++) {
					offset += 0.7 * _RandomOffsets.w;

					fixed3 layerCol = tex2D(_Spectrum, spec).rgb;

					float layerPointDensity = calcLayer(uv, offset);
					backgroundAlpha = saturate(backgroundAlpha + layerPointDensity);
					col = lerp(col, layerCol, layerPointDensity);

					spec += 0.7;
				}
				col = lerp(tex2D(_Background, uv).rgb, col, backgroundAlpha);

				return col;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				// calculate triplanar blend
				half3 triblend = saturate(pow(i.worldNormal, 2)); // 2 was 4
				triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);
				//return fixed4(triblend.xyz, 1);

				// calculate triplanar uvs
				// applying texture scale and offset values ala TRANSFORM_TEX macro
				float2 uvX = i.worldPos.zy;
				float2 uvY = i.worldPos.xz;
				float2 uvZ = i.worldPos.xy;
				// offset UVs to prevent obvious mirroring
				#if defined(TRIPLANAR_UV_OFFSET)
					uvY += 0.33;
					uvZ += 0.67;
				#endif

				// minor optimization of sign(). prevents return value of 0
				half3 axisSign = i.worldNormal < 0 ? -1 : 1;

				// flip UVs horizontally to correct for back side projection
				#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
					uvX.x *= axisSign.x;
					uvY.x *= axisSign.y;
					uvZ.x *= -axisSign.z;
				#endif

				// tangent space normal maps
				float offsetX = 0.22 * axisSign.x + _RandomOffsets.x;
				float offsetY = 0.37 * axisSign.y + _RandomOffsets.y;
				float offsetZ = 0.46 * axisSign.z + _RandomOffsets.z;

				fixed3 colX = calculateSide(uvX * 1.15 + offsetX);
				fixed3 colY = calculateSide(uvY * 1.075 + offsetY);
				fixed3 colZ = calculateSide(uvZ + offsetZ);

				fixed3 col = (
					colX.rgb * triblend.x +
					colY.rgb * triblend.y +
					colZ.rgb * triblend.z
				);

				return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
