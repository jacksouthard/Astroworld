using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class BackgroundGenerator : MonoBehaviour
{
	public int width = 1024;

	public Color backgroundColor;

	[Header("Point Stars")]
	public bool regeneratePointStars;
	public float starDensity;
	public float starBrightness;

	// references
	MeshRenderer backgroundSphere;

	private void Start () {
		// get references
		backgroundSphere = transform.Find("BackgroundSphere").GetComponent<MeshRenderer>();

		if (regeneratePointStars) {
			Texture2D pointStars = StarGenerator.GeneratePointStars(width, starDensity, starBrightness, backgroundColor);
			backgroundSphere.material.mainTexture = pointStars;

			// save the texture
			SaveTextureAsPNG(pointStars, "pointStars");
		}
		
	}

	const string savePath = "/Resources/SavedBackgrounds/";
	public static void SaveTextureAsPNG (Texture2D texture, string fileName = "map") {
		//first Make sure you're using RGB24 as your texture format
		//Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

		//then Save To Disk as PNG
		byte[] bytes = texture.EncodeToPNG();
		string dirPath = Application.dataPath + savePath;
		if (!Directory.Exists(dirPath)) {
			Directory.CreateDirectory(dirPath);
		}
		File.WriteAllBytes(dirPath + fileName + ".png", bytes);
	}
}
