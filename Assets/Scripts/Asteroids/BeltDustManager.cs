using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltDustManager : MonoBehaviour
{
    public GameObject dustPrefab;
    const int dustWallCount = 3;
    const float avgGapBetweenDust = 50;
    const float gapPercentVariance = 0.3f;
    const float minGap = avgGapBetweenDust * (1 - gapPercentVariance);
    const float maxGap = avgGapBetweenDust * (1 + gapPercentVariance);
    const float maxDistanceFromCamera = (dustWallCount - 1) * maxGap;

    List<BeltDustWall> dustWalls = new List<BeltDustWall>();

    const float generationStartZ = 30;
    float generatedZ = generationStartZ;

    Vector4 lastRandomOffset;
    const float randomOffsetMaxVariance = 0.04f;
    
    // visibility
    public AnimationCurve visibilityCurve;

    public void GenerateInitial () {
        lastRandomOffset = new Vector4(Random.value * 100, Random.value * 100, Random.value * 100, Random.value * 100);
        for (int i = 0; i < dustWallCount; i++) {
            GenerateNext();
        }
    }

    public void CurrentZUpdated (float z) {

    }

    public void FarthestZUpdated (float farthestZ) {
        if (dustWalls[0].z < farthestZ) {
            DespawnLast();
            GenerateNext();
        }

        UpdateVisibilities(farthestZ);
    }

    void GenerateNext () {
        BeltDustWall newWall = Instantiate(dustPrefab, Vector3.forward * generatedZ, Quaternion.identity, transform).GetComponent<BeltDustWall>();
        newWall.Initialize(generatedZ, lastRandomOffset);
        dustWalls.Add(newWall);

        generatedZ += Random.Range(minGap, maxGap);

        // permutate random offset
        lastRandomOffset += randomOffsetMaxVariance * new Vector4(Random.value, Random.value, Random.value, Random.value);
    }

    void DespawnLast () {
        Destroy(dustWalls[0].gameObject);
        dustWalls.RemoveAt(0);
    }

    void UpdateVisibilities (float farthestZ) {
        foreach (var dustWall in dustWalls) {
            float distanceRatio = (dustWall.z - farthestZ) / maxDistanceFromCamera;
            dustWall.SetVisibility(visibilityCurve.Evaluate(distanceRatio));
        }
    }
}
