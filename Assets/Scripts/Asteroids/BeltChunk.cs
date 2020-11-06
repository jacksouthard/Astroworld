using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltChunk : MonoBehaviour
{
    public const float generationZStep = BeltGenerator.chunkSize / (BeltGenerator.asteroidsPerChunk);

    public enum LOD {
        movingAndPhysical,
        moving,
        billboard,
        unset
    }
    LOD curLOD = LOD.unset;

    Vector3[] asteroidPositions;
    List<GameObject> asteroids = new List<GameObject>();

    public float startZ { get; private set; }

    // when chunk is created, it is automattically positioned to its generation start position
    public void Initialize () {
        asteroidPositions = new Vector3[BeltGenerator.asteroidsPerChunk];
        startZ = transform.position.z;
        float curZ = startZ;
        for (int i = 0; i < BeltGenerator.asteroidsPerChunk; i++) {
            curZ += generationZStep;
            asteroidPositions[i] = GetNextAsteroidPosition(curZ);
        }
    }

    public void Despawn () {
        Destroy(gameObject);
    }

    // LOD switching
    public void SetLOD(LOD newLOD) {
        if (curLOD == newLOD) {
            //Debug.LogWarning(name + " is trying to switch to " + newLOD + " but is already set at that");
            return;
        }
        // exit old state
        switch (curLOD) {
            case LOD.billboard:
                ExitBillboard();
                break;
            case LOD.moving:
                break;
            case LOD.movingAndPhysical:
                break;
            case LOD.unset:
                break;
        }

        // enter new state
        curLOD = newLOD;
        switch(newLOD) {
            case LOD.billboard:
                EnterBillboard();
                break;
            case LOD.moving:
                EnterMoving();
                break;
            case LOD.movingAndPhysical:
                EnterMovingAndPhysical();
                break;
            case LOD.unset:
                break;
        }
    }

    // LOD states
    void EnterBillboard () {
        AsteroidManager.instance.RegisterBillboardAsteroids(asteroidPositions);
    }
    void ExitBillboard () {
        AsteroidManager.instance.DeregisterBillboardAsteroids(asteroidPositions[0], BeltGenerator.asteroidsPerChunk);
    }

    void EnterMoving () {
        Generate3DAsteroids();
        AsteroidManager.instance.GiveAsteroidsRigidBodies(asteroids);
        RandomizeAsteroidMotion();
    }

    void EnterMovingAndPhysical () {
        if (asteroids.Count == 0) EnterMoving();
        SetAsteroidCollidersActive(true);
    }

    // helpers
    void Generate3DAsteroids () {
        for (int i = 0; i < BeltGenerator.asteroidsPerChunk; i++) {
            asteroids.Add(AsteroidManager.instance.GenerateAsteroid(asteroidPositions[i], this));
        }
        asteroidPositions = null;
    }

    void SetAsteroidCollidersActive (bool newActive) {
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).GetComponent<Collider>().enabled = newActive;
        }
    }

    void RandomizeAsteroidMotion () {
        foreach (var asteroid in asteroids) {
            Rigidbody rb = asteroid.GetComponent<Rigidbody>();
            // do something
        }
    }

    Vector3 GetNextAsteroidPosition (float z) {
        Vector2 xyPos = Random.insideUnitCircle * BeltGenerator.beltRadius;
        return new Vector3(xyPos.x, xyPos.y, z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        Vector3 pos = transform.position + Vector3.forward * BeltGenerator.chunkSize / 2f;
        Gizmos.DrawWireCube(pos, new Vector3(BeltGenerator.beltRadius * 2f, BeltGenerator.beltRadius * 2f, BeltGenerator.chunkSize));
    }
#endif
}
