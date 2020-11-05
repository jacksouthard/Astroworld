Shader "Custom/Planet"
{
    Properties
    {
		_Color("Color", Color) = (0, 0, 0, 1)
		_BaseLayerColor("Base Layer Color", Color) = (0, 0, 0, 1)
		[Toggle(ADDLAYER)] _DoAddLayer("Do Add Layer", Float) = 0
		_Add0LayerColor("Add 0 Layer Color", Color) = (0, 0, 0, 1)
		_Scale("Scale", Range(0,3)) = 1
		_Squish("Squish", Range(0,15)) = 1
		_Roundness("Roundness", Range(0,10)) = 4
		_Density("Density", Range(0,10)) = 0.5
		_DistortionScale("Distortion Scale", Range(0,3)) = 1
		_Distortion("Distortion", Range(0, 1)) = 1
		[Toggle(TOON)] _DoToon("Do Toon Shading", Float) = 0
		[NoScaleOffset]_Noise("Noise", 2D) = "white" {}
		[NoScaleOffset]_DistortionNoise("Distorition Noise", 2D) = "white" {}
		[NoScaleOffset]_AddMask("Add Mask", 2D) = "white" {}
		[Toggle(ADDMASK)] _DoMask2("Do Add Mask", Float) = 0
		_MaskScale("Mask Scale", Range(0,1)) = 0.5
		_MaskCutoffLow("Mask Cutoff Low", Range(0,1)) = 0.3
		_MaskCutoffHigh("Mask Cutoff High", Range(0,1)) = 0.7
		_AddCutoff("Add Cutoff", Range(0,1)) = 0.5
		_AtmosphereColor("Atmosphere Color", Color) = (0, 0, 0, 1)
		_AtmosphereFalloff("Atmosphere Falloff", Range(1,10)) = 3
		_AtmosphereInflate("Atmosphere Size", Range(0,0.25)) = 0.1
		_RandomOffsets("Random Offsets", Vector) = (0, 0, 0, 0)
	}
    SubShader
    {
		Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha // Additive blending.

        Pass
        {
			//ZWrite On
			Tags {"LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			#pragma multi_compile_local _ TOON
			#pragma multi_compile_local _ ADDLAYER
			#pragma multi_compile_local _ ADDMASK

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
			sampler2D _DistortionNoise;
			
			sampler2D _AddMask;
			half _MaskScale;
			half _MaskCutoffLow;
			half _MaskCutoffHigh;
			half _AddCutoff;

			half4 _Color;
			half4 _BaseLayerColor;
			half4 _Add0LayerColor;
			half _Scale;
			half _Squish;
			half _Density;
			half _Roundness;
			half _DistortionScale;
			half _Distortion;

			fixed4 _RandomOffsets;

			// lighting
			fixed4 _LightColor0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			float noise(float2 p) {
				return tex2D(_Noise, p).a;
			}
			float noise2(float2 p) {
				return tex2D(_DistortionNoise, p).a;
			}
			float noise3(float2 p) {
				return tex2D(_AddMask, p).a;
			}

			float toonRamp(float val) {
				const float toonCuts = 7;
				return floor(val * toonCuts) / toonCuts;
			}
			float toonRampCustom(float val, int cuts) {
				return floor(val * cuts) / cuts;
			}
			float smoothRamp(float val) {
				if (val < _AddCutoff) {
					val = 0;
				}
				else {
					val = (val - _AddCutoff) / (1 - _AddCutoff);
				}
				return val;
			}
			float toonRampShading(float val) {
				const float absoluteMin = 0.6;
				const float boostVal = 0.9;
				const float boostRatio = 1.2;
				val = saturate(val * boostRatio + boostVal);
				const float toonCuts = 1;
				val = floor(val * toonCuts) / toonCuts;
				if (val < absoluteMin) val = absoluteMin;
				return val;
			}

			float calcMask(float2 uv, float2 offset) {
				float m = 1 - noise3(uv * _MaskScale * _Scale + offset);
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

			float calcCircle(half x, half y) {
				return sqrt(1 - x * x - y * y);
			}
			float calcSphereShading(float2 trueUV) {
				half xSample = (trueUV.x - 0.5) * 2;
				half ySample = (trueUV.y - 0.5) * 2;
				half3 localNormal = float3(xSample, ySample, calcCircle(xSample, ySample));
				localNormal.z *= -1;
				half3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, localNormal));

				return dot(worldNormal, _WorldSpaceLightPos0.xyz);// +rim);
				//ndotl = saturate((ndotl + 0.1) * 2);
				//ndotl = toonRamp(ndotl);
			}

			float calcLayer(float2 uv, float2 offset) {
				float distortionX = noise2(uv * _DistortionScale * _Scale + offset);
				float distortionY = noise2(uv.yx * _DistortionScale * _Scale * 1.5f + offset);
				float n = noise(uv * _Scale + float2(distortionX, distortionY) * _Distortion + offset);
				#ifdef TOON
				n = toonRamp(n);
				#endif
				return n * _Density;
			}
			float calcAddLayer(float2 uv, float2 offset) {
				float distortionX = noise2(uv * _DistortionScale * _Scale + offset);
				float distortionY = noise2(uv.yx * _DistortionScale * _Scale * 1.5f + offset);
				float n = noise(uv * _Scale + float2(distortionX, distortionY) * _Distortion + offset);
				//n = 1 - n;

				n = smoothRamp(n);
#ifdef ADDMASK
				n *= calcMask(uv, offset);
#endif

				//#ifdef MASK2
				//				n *= calcMask2(uv, offset * 1.3 + 0.7);
				//#endif
				#ifdef TOON
				n = toonRamp(n);
				#endif
				return n * _Density;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float2 uv = i.uv;
				float2 trueUV = i.uv;

				float2 toCenter = uv - float2(0.5, 0.5);
				float p = length(toCenter) * 2;// / 1.41421356;
				clip(1 - p);

				float squishPercent = pow(p, _Roundness);
				uv += toCenter * squishPercent;
				uv.y *= _Squish;
				
				float backgroundAlpha = 0;
				fixed3 col = _Color;

				float2 offset = _RandomOffsets.xy;

				float layerPointDensity = calcLayer(uv, offset);
				backgroundAlpha = saturate(backgroundAlpha + layerPointDensity);
				col = lerp(col, _BaseLayerColor, layerPointDensity);

				#ifdef ADDLAYER
				const int steps = 1;
				for (int i = 0; i < steps; i++) {
					offset += 0.7 *_RandomOffsets.w;
								
					float layerPointDensity = calcAddLayer(uv, offset) * _Add0LayerColor.a;
					backgroundAlpha = saturate(backgroundAlpha + layerPointDensity);
					col = lerp(col, _Add0LayerColor.rgb, layerPointDensity);
				}
				#endif
				col = lerp(_Color, col, backgroundAlpha);

				// do shading
				float ndotl = calcSphereShading(trueUV);
				#ifdef TOON
				ndotl = toonRampShading(ndotl);
				#endif

				return fixed4(col.rgb * ndotl, 1);
            }
            ENDCG
        }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

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

			half4 _AtmosphereColor;
			half _AtmosphereInflate;
			half _AtmosphereFalloff;

			v2f vert(appdata v)
			{
				v2f o;
				float2 toCenter = v.uv - float2(0.5, 0.5);
				v.vertex.xy += toCenter * _AtmosphereInflate;
				v.vertex.z -= 0.001;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = v.uv;
				return o;
			}

			float toonRamp(float val) {
				const float toonCuts = 7;
				return floor(val * toonCuts) / toonCuts;
			}

			float calcCircle(half x, half y) {
				return sqrt(1 - x * x - y * y);
			}
			float calcSphereShading(float2 trueUV) {
				half xSample = (trueUV.x - 0.5) * 2;
				half ySample = (trueUV.y - 0.5) * 2;
				half3 localNormal = float3(xSample, ySample, calcCircle(xSample, ySample));
				localNormal.z *= -1;
				half3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, localNormal));

				return saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));// +rim);
				//ndotl = saturate((ndotl + 0.1) * 2);
				//ndotl = toonRamp(ndotl);
			}


			fixed4 frag(v2f i) : SV_Target
			{
				float p = length(i.uv - float2(0.5, 0.5)) * 2;// / 1.41421356;
				clip(1 - p);
				float fallOffTransitionValue = 1 - _AtmosphereInflate;
				const float innerFallOffEnd = 0.8;
				float a;
				if (p > fallOffTransitionValue) {
					// do falloff
					a = 1 - (p - fallOffTransitionValue) / (1 - fallOffTransitionValue);
					
				}
				else {
					a = saturate((p - innerFallOffEnd) / (fallOffTransitionValue - innerFallOffEnd));
					
				}
				a = pow(a, _AtmosphereFalloff);
#ifdef TOON
				const int toonCuts = 3;
				a = floor(a * toonCuts) / toonCuts;
#endif
				float ndotl = calcSphereShading(i.uv);
				return fixed4(_AtmosphereColor.rgb, _AtmosphereColor.a * a * ndotl);
			}
			ENDCG
		}
    }
}
