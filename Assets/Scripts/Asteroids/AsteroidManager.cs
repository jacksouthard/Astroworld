using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class AsteroidManager : Singleton<AsteroidManager>
{
    private void Awake() {
        InitializeAsteroidDatas();
        InitializeBillboardAssets();
        InitializeBillboards();
    }

    // ASTEROID DATA
    public AsteroidData[] asteroidDatas;

    [System.Serializable]
    public struct AsteroidData {
        public GameObject prefab;
        [HideInInspector]
        public float squareSize;
    }

    void InitializeAsteroidDatas () {
        for (int i = 0; i < asteroidDatas.Length; i++) {
            float radius = asteroidDatas[i].prefab.GetComponent<SphereCollider>().radius;
            asteroidDatas[i].squareSize = radius * 2f;
        }
    }

    // 3D ASTEROID GENERATION
    public GameObject GenerateAsteroid (AsteroidBillboard billboard, BeltChunk chunk) {
        AsteroidData asteroidData = asteroidDatas[billboard.atlasIndex];
        GameObject ret = Instantiate(asteroidData.prefab, billboard.position, Quaternion.identity, chunk.transform);
        ret.transform.localScale = Vector3.one * billboard.size;
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

    // ASTEROID SCALES
    [Header("Asteroid Generation")]
    public float minSize = 0.5f;
    public float maxSize = 2f;
    public AnimationCurve sizeCurve;

    public float GetRandomAsteroidSize () {
        return Mathf.Lerp(minSize, maxSize, sizeCurve.Evaluate(Random.value));
    }

    // ASTEROID PHYSICS
    [Header("Moving Asteroids")]
    public float avgSpinSpeed = 1;
    const float spinSpeedPercentVariance = 0.6f;
    const float upperSpeedPercent = 1f + spinSpeedPercentVariance;
    const float lowerSpeedPercent = 1f - spinSpeedPercentVariance;

    public void ApplyImpulseToAsteroid(Rigidbody asteroidRB) {
        float spinSpeed = Random.Range(lowerSpeedPercent, upperSpeedPercent) * avgSpinSpeed;
        asteroidRB.AddTorque(Random.onUnitSphere * spinSpeed, ForceMode.VelocityChange);
    }

    // PARTICLE BILLBOARDS
    public struct AsteroidBillboard {
        public int atlasIndex;
        public Vector3 position;
        public float size;

        public AsteroidBillboard(int atlasIndex, Vector3 position, float size) {
            this.atlasIndex = atlasIndex;
            this.position = position;
            this.size = size;
        }
    }

    [Header("Billboards")]
    public GameObject billboardParticlesPrefab;
    AsteroidBillboardParticles[] asteroidBillboards;
    // list is sorted with lowest z billboard asteroids first (index 0)
    List<AsteroidBillboard> activeAsteroidBillboards = new List<AsteroidBillboard>();
    
    void InitializeBillboards () {
        asteroidBillboards = new AsteroidBillboardParticles[asteroidDatas.Length];
        for (int i = 0; i < asteroidDatas.Length; i++) {
            asteroidBillboards[i] = Instantiate(billboardParticlesPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<AsteroidBillboardParticles>();
            asteroidBillboards[i].gameObject.name = "Billboards" + i;
            asteroidBillboards[i].Initialize(asteroidDatas[i], i);
        }
    }

    public void RegisterBillboardAsteroids (ref AsteroidBillboard[] newBillboards) {
        for (int i = 0; i < newBillboards.Length; i++) {
            activeAsteroidBillboards.Add(newBillboards[i]);
            asteroidBillboards[newBillboards[i].atlasIndex].PushBack(newBillboards[i]);
        }
    }

    public void DeregisterBillboardAsteroids(int registeredCount) {
        for (int i = 0; i < registeredCount; i++) {
            asteroidBillboards[activeAsteroidBillboards[i].atlasIndex].PopFront();
        }
        activeAsteroidBillboards.RemoveRange(0, registeredCount);
    }

    // BILLBOARD CAPTURING
    [Header("Billboard Capturing")]
    public bool recaptureAsteroids;
    public bool squareAtlas = true;
    public Texture altasTexture;
    const int captureLayer = 9;
    const int captureTextureSize = 128; // per asteroid
    public static int altasSideSize;
    public static int altasTotalSize;
    Transform captureSpace;
    Camera captureCamera;

    void InitializeBillboardAssets () {
        captureSpace = transform.Find("CaptureSpace");  
        captureCamera = captureSpace.Find("CaptureCamera").GetComponent<Camera>();

        if (squareAtlas) {
            altasSideSize = Mathf.CeilToInt(Mathf.Sqrt(asteroidDatas.Length));
            altasTotalSize = altasSideSize * altasSideSize;
        } else {
            altasSideSize = asteroidDatas.Length;
            altasTotalSize = altasSideSize;
        }

        if (recaptureAsteroids) {
            // generate a new atlas
            if (squareAtlas) altasTexture = CaptureSquareAsteroidAtlas();
            else altasTexture = CaptureCollumnAsteroidAtlas();
        }

        Destroy(captureSpace.gameObject);
    }

    Texture2D CaptureSquareAsteroidAtlas () {
        // set up the asteroid prefabs to be captured
        // each asteroid will be scaled to have a width of 1m
        float offset1D = (float)(altasSideSize - 1) / 2f;
        Vector3 topLeft = new Vector3(-offset1D, offset1D, 0);
        for (int i = 0; i < asteroidDatas.Length; i++) {
            Vector2Int coords = GetAtlasCoordsFromIndex(i);
            Vector3 pos = topLeft + new Vector3(coords.x, -coords.y, 0);

            GameObject captureObject = Instantiate(asteroidDatas[i].prefab, pos, Quaternion.identity, captureSpace);
            captureObject.layer = captureLayer;
            captureObject.transform.localScale = Vector3.one * (1f / asteroidDatas[i].squareSize);
        }

        int texSize = captureTextureSize * altasSideSize;
        RenderTexture renderTexture = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGB32, 0);
        // setup the camera
        captureCamera.orthographicSize = altasSideSize / 2f;
        captureCamera.aspect = 1;
        // render it to the render texture
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        // copy the render texture to a texture
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, texSize, texSize), 0, 0);
        texture.Apply(true);
        SaveTextureAsPNG(texture, "AsteroidAtlas");

        RenderTexture.active = null;
        captureCamera.targetTexture = null;

        Debug.Log("Captured " + altasSideSize + "x" + altasSideSize + " Atlas with " + asteroidDatas.Length + " asteroids");

        return texture;    
    }

    Texture2D CaptureCollumnAsteroidAtlas() {
        // set up the asteroid prefabs to be captured
        // each asteroid will be scaled to have a width of 1m
        float offset1D = (float)(altasSideSize - 1) / 2f;
        Vector3 topLeft = new Vector3(0, offset1D, 0);
        for (int i = 0; i < asteroidDatas.Length; i++) {
            Vector3 pos = topLeft + new Vector3(0, -i, 0);

            GameObject captureObject = Instantiate(asteroidDatas[i].prefab, pos, Quaternion.identity, captureSpace);
            captureObject.layer = captureLayer;
            captureObject.transform.localScale = Vector3.one * (1f / asteroidDatas[i].squareSize);
        }

        Vector2Int texSize = new Vector2Int(captureTextureSize, captureTextureSize * altasSideSize);
        RenderTexture renderTexture = new RenderTexture(texSize.x, texSize.y, 0, RenderTextureFormat.ARGB32, 0);
        // setup the camera
        float worldwidth = 0.5f;
        float worldHeight = altasTotalSize / 2f;
        captureCamera.orthographicSize = worldHeight;
        captureCamera.aspect = worldwidth / worldHeight;
        // render it to the render texture
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        // copy the render texture to a texture
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(texSize.x, texSize.y, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, texSize.x, texSize.y), 0, 0);
        texture.Apply(true);
        SaveTextureAsPNG(texture, "AsteroidAtlas");

        RenderTexture.active = null;
        captureCamera.targetTexture = null;

        Debug.Log("Captured 1x" + altasSideSize + " Atlas with " + asteroidDatas.Length + " asteroids");

        return texture;
    }

    const float particleLifeTime = 1000;
    float GetLifeTimeForAtlasIndex (int index) {
        return (((float)index + 0.5f) / (float)altasTotalSize) * particleLifeTime;
    }

    Vector2Int GetAtlasCoordsFromIndex (int index) {
        int y = 0;
        while (index >= altasSideSize) {
            index -= altasSideSize;
            y++;
        }
        return new Vector2Int(index, y);
    }
    public int GetRandomAtlasIndex () {
        return Random.Range(0, asteroidDatas.Length);
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
