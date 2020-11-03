﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StarGenerator
{
	public static Texture2D GeneratePointStars (int width, float density, float brightness, Color backgroundColor) {
		Texture2D texture = new Texture2D(width, width, TextureFormat.RGBA32, false);
		Color32[] color32s = new Color32[width * width];
		for (int y = 0; y < width; y++) {
			for (int x = 0; x < width; x++) {
				byte r = (byte)(backgroundColor.r * 255f);
				byte g = (byte)(backgroundColor.g * 255f);
				byte b = (byte)(backgroundColor.b * 255f);
				byte a = 1;
				color32s[y * width + x] = new Color32(r, g, b, a);
			}
		}

		int count = (int)((float)(width * width) * density);
		for (int i = 0; i < count; i++) {
			int x = Random.Range(0, width);
			int y = Random.Range(0, width);
			float val = Mathf.Log10(Random.value + 0.0001f) * -brightness;
			byte bVal = (byte)(val * 255f);
			color32s[y * width + x] = new Color32(bVal, bVal, bVal, bVal);
		}
		texture.SetPixels32(color32s);
		texture.Apply(false, false);
		return texture;
	}
}
