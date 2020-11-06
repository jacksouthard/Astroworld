using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class AsteroidManager : Singleton<AsteroidManager>
{
    private void Awake() {
        InitializeBillboardAssets();
    }

    // 3D ASTEROID GENERATION
    public GameObject asteroidPrefab;
    public const float asteroidRadius = 1.3f;

    public GameObject GenerateAsteroid (Vector3 position, BeltChunk chunk) {
        GameObject ret = Instantiate(asteroidPrefab, position, Quaternion.identity, chunk.transform);
        return ret;
    }

    public void GiveAsteroidsRigidBodies (List<GameObject> asteroids) {
        foreach (var asteroid in asteroids) {
            Rigidbody newRB = asteroid.AddComponent<Rigidbody>();
            newRB.useGravity = false;
            newRB.drag = 0;
            newRB.angularDrag = 0;
            newRB.mass = 500;
        }
    }

    // PARTICLE BILLBOARDS
    public ParticleSystem billboardPS;
    bool particleUpdateQued = false;
    const int maxActiveAsteroids = 500; // not synced with particle system max particles, but they should be the same

    // list is sorted with lowest z billboard asteroids first (index 0)
    List<Vector3> activeAsteroidPositions = new List<Vector3>();
    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[maxActiveAsteroids];

    public void RegisterBillboardAsteroids (Vector3[] asteroidPositions) {
        for (int i = 0; i < asteroidPositions.Length; i++) {
            activeAsteroidPositions.Add(asteroidPositions[i]);
        }

        QueParticleUpdate();
    }

    public void DeregisterBillboardAsteroids(Vector3 firstAsteroidPosition, int registeredCount) {
        int firstIndex = activeAsteroidPositions.IndexOf(firstAsteroidPosition);
        activeAsteroidPositions.RemoveRange(firstIndex, registeredCount);
        QueParticleUpdate();
    }

    void QueParticleUpdate () {
        if (particleUpdateQued) return;
        StartCoroutine(UpdateParticlesNextFrame());

    }
    IEnumerator UpdateParticlesNextFrame () {
        particleUpdateQued = true;
        yield return null;
        UpdateParticles();
        particleUpdateQued = false;
    }

    void UpdateParticles () {
        int diff = activeAsteroidPositions.Count - billboardPS.particleCount;
        //print("Old: " + billboardPS.particleCount + " Diff: " + diff);
        if (diff > 0) {
            // particle system has less particles than active asteroids
            billboardPS.Emit(diff);
            // then re get the particles array
            //billboardPS.GetParticles(particles);
        } else if (diff < 0) {
            // particle system has more particles than active asteroids
            int destroyStartIndex = billboardPS.particleCount + diff;
            for (int j = 0; j < billboardPS.particleCount; j++) {
                particles[j].remainingLifetime = -1;
            }
        }

        //print("New: " + billboardPS.particleCount);
        int i = 0;
        foreach (var asteroidPos in activeAsteroidPositions) {
            particles[i] = new ParticleSystem.Particle {
                position = asteroidPos,
                startSize = 2 * asteroidRadius,
                startColor = Color.white,
                startLifetime = Mathf.Infinity
            };
            i++;
        }

        billboardPS.SetParticles(particles, activeAsteroidPositions.Count);
    }

    // BILLBOARD CAPTURING
    [Header("Billboard Capturing")]
    public bool recaptureAsteroids;
    Transform captureSpace;
    Camera captureCamera;
    const int captureLayer = 9;
    const int captureTextureSize = 256;
    void InitializeBillboardAssets () {
        captureSpace = transform.Find("CaptureSpace");  
        captureCamera = captureSpace.Find("CaptureCamera").GetComponent<Camera>();

        if (recaptureAsteroids) {
            CaptureAsteroid(asteroidPrefab);
        }

        Destroy(captureSpace.gameObject);
    }

    void CaptureAsteroid (GameObject capturePrefab) {
        GameObject captureObject = Instantiate(capturePrefab, captureSpace.position, Quaternion.identity, captureSpace);
        captureObject.layer = captureLayer;

        RenderTexture renderTexture = new RenderTexture(captureTextureSize, captureTextureSize, 0, RenderTextureFormat.ARGB32, 0);
        // setup the camera
        captureCamera.orthographicSize = asteroidRadius;
        captureCamera.aspect = 1;
        // render it to the render texture
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        // copy the render texture to a texture
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(captureTextureSize, captureTextureSize, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, captureTextureSize, captureTextureSize), 0, 0);
        texture.Apply(true);
        SaveTextureAsPNG(texture, capturePrefab.name);

        RenderTexture.active = null;
        captureCamera.targetTexture = null;

        // Destroy the gameobject
        DestroyImmediate(captureObject);

        Debug.Log("Captured  " + capturePrefab.name);
    }

    const string savePath = "/Resources/SavedAsteroids/";
    public static void SaveTextureAsPNG(Texture2D texture, string fileName) {
        byte[] bytes = texture.EncodeToPNG();
        string dirPath = Application.dataPath + savePath;
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + fileName + ".png", bytes);
    }
}
