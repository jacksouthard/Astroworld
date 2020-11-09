using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltGenerator : MonoBehaviour
{
    public GameObject chunkPrefab;

    public const float gapBetweenAsteroids = 5; // 10
    public const float asteroidGapVariance = 0.25f; // percent
    public const float chunkSize = 50;
    public const int asteroidsPerChunk = (int)(chunkSize / gapBetweenAsteroids + 0.9f);
    public const float beltRadius = 25;

    const float generationStartZ = -50;
    float generatedZ = generationStartZ;
    float despawnedZ = generationStartZ;
    float farthestZ = Mathf.NegativeInfinity;

    // generation buffers
    public const float generationBuffer = 1500;
    public const float despawnBuffer = 75;
    // lod switch distances
    public const float movingAsteroidDistance = 200;
    public const float movingAndPhysicalAsteroidDistance = 100;

    // belt is made up of a list of chunks, index 0 is lowest Z and end index is highest z
    List<BeltChunk> chunks = new List<BeltChunk>();

    // references
    Transform mainCam;
    BeltDustManager beltDust;

    private void Start() {
        // get references
        mainCam = Camera.main.transform;
        beltDust = transform.Find("BeltDust").GetComponent<BeltDustManager>();

        GenerateInitial();
    }

    // testing
    private void Update() {
        mainCam.position = Vector3.forward * (Time.time * 25f);
        Debug.DrawRay(mainCam.position, Vector3.up * 30, Color.yellow, Time.deltaTime);
        Debug.DrawRay(mainCam.position + Vector3.forward * movingAsteroidDistance, Vector3.up * 30, Color.red, Time.deltaTime);
        SetFarthestZ(mainCam.position.z);
    }

    public void GenerateInitial () {
        beltDust.GenerateInitial();
        SetFarthestZ(0);
    }

    public void SetFarthestZ (float z) {
        if (z > farthestZ) farthestZ = z;

        while (farthestZ + generationBuffer > generatedZ) {
            GenerateNext();
        }
        while (farthestZ - despawnBuffer - chunkSize > despawnedZ) {
            DespawnLast();
        }

        UpdateLODs();

        beltDust.FarthestZUpdated(z);
    }

    void GenerateNext () {
        BeltChunk newChunk = Instantiate(chunkPrefab, Vector3.forward * generatedZ, Quaternion.identity, transform).GetComponent<BeltChunk>();
        newChunk.Initialize();
        chunks.Add(newChunk);

        generatedZ += chunkSize;
    }

    void DespawnLast () {
        chunks[0].Despawn();
        chunks.RemoveAt(0);

        despawnedZ += chunkSize;
    }

    void UpdateLODs () {
        //print (farthestZ - despawnedZ);
        float curDist = -despawnBuffer - chunkSize;
        foreach (var chunk in chunks) {
            BeltChunk.LOD newLOD;
            if (curDist < movingAndPhysicalAsteroidDistance) {
                newLOD = BeltChunk.LOD.movingAndPhysical;
            } else if (curDist < movingAsteroidDistance) {
                newLOD = BeltChunk.LOD.moving;
            } else {
                newLOD = BeltChunk.LOD.billboard;
            }
            chunk.SetLOD(newLOD);

            curDist += chunkSize;
        }
    }
}
