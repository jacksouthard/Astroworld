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

	[Header("Planet")]
	public float maxPlanetAngleFromBottom = 15;
	public Material[] planetMaterials;
	public PlanetColorData[] planetDatas;

	[Header("Background Capture")]
	public Cubemap backgroundCubemap;

	// references
	MeshRenderer backgroundSphere;
	Material nebulaMat;
	Transform planetAnchor;
	Material planetMat;

	private void Start () {
		// get references
		backgroundSphere = transform.Find("BackgroundSphere").GetComponent<MeshRenderer>();
		nebulaMat = backgroundSphere.sharedMaterial;
		planetAnchor = transform.Find("PlanetAnchor");
		MeshRenderer planetMR = planetAnchor.GetComponentInChildren<MeshRenderer>();
		//planetMat = new Material(planetMR.sharedMaterial);
		planetMat = new Material(planetMaterials[Random.Range(0, planetMaterials.Length)]);
		planetMR.material = planetMat;

		if (regeneratePointStars) {
			Texture2D pointStars = StarGenerator.GeneratePointStars(width, starDensity, starBrightness, backgroundColor);
			backgroundSphere.material.mainTexture = pointStars;

			// save the texture
			SaveTextureAsPNG(pointStars, "pointStars");
		}

		RandomizeNebula();
		RandomizePlanet();

		//CaptureBackground();
		StartCoroutine(CaptureNextFrame());
	}

	IEnumerator CaptureNextFrame () {
		yield return new WaitForEndOfFrame();
		CaptureBackground();
	}

	void CaptureBackground () {
		Camera captureCamera = transform.Find("CaptureCamera").GetComponent<Camera>();
		captureCamera.RenderToCubemap(backgroundCubemap);

		Destroy(gameObject);
	}

	void RandomizeNebula () {
		nebulaMat.SetVector("_RandomOffsets", GetRandomOffset());
	}

	void RandomizePlanet () {
		planetMat.SetVector("_RandomOffsets", GetRandomOffset());

		// calculate random rotation
		float xAngle = 0;// Random.value * maxPlanetAngleFromBottom;
		float yAngle = Random.value * 360f;
		planetAnchor.rotation = Quaternion.Euler(xAngle, yAngle, 0);
		float planetScale = GetVariedValue(1f, overallSizePercentVariance);
		planetAnchor.localScale = new Vector3(planetScale, 1, planetScale);

		PlanetColorData colorData = planetDatas[Random.Range(0, planetDatas.Length)];
		PermutePlanet(planetMat, ref colorData);
	}

	Vector4 GetRandomOffset () {
		return new Vector4(Random.value, Random.value, Random.value, Random.value) * 100f;
	}

	[System.Serializable]
	public struct PlanetColorData {
		public Color baseColor;
		public Color baseLayerColor;
		public Color addLayerColor;
		public Color atmosphereColor;
	}

	const float overallSizePercentVariance = 0.3f;
	const float scalePercentVariance = 0.25f;
	const float squishPercentVariance = 0.25f;
	const float densityPercentVariance = 0.1f;
	const float distortionScalePercentVariance = 0.25f;
	const float distortionPercentVariance = 0.25f;
	const float atmoSizePercentVariance = 0.25f;
	const float atmoFalloffPercentVariance = 0.25f;
	static void PermutePlanet (Material mat, ref PlanetColorData planetColorData) {
		mat.SetColor("_Color", planetColorData.baseColor);
		mat.SetColor("_BaseLayerColor", planetColorData.baseLayerColor);
		mat.SetColor("_Add0LayerColor", planetColorData.addLayerColor);
		mat.SetColor("_AtmosphereColor", planetColorData.atmosphereColor);

		PermutePram(mat, "_Scale", scalePercentVariance);
		PermutePram(mat, "_Squish", squishPercentVariance);
		PermutePram(mat, "_Density", densityPercentVariance);
		PermutePram(mat, "_DistortionScale", distortionScalePercentVariance);
		PermutePram(mat, "_Distortion", distortionPercentVariance);
		PermutePram(mat, "_AtmosphereInflate", atmoSizePercentVariance);
		PermutePram(mat, "_AtmosphereFalloff", atmoFalloffPercentVariance);
	}

	static void PermutePram (Material mat, string pram, float maxVariance) {
		mat.SetFloat(pram, GetVariedValue(mat.GetFloat(pram), maxVariance));
	}

	static float GetVariedValue(float initial, float maxPercent) {
		return initial * Random.Range(1f - maxPercent, 1f + maxPercent);
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
