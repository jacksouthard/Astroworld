using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidBillboardParticles : MonoBehaviour
{
    const int maxActiveAsteroids = 500;
    const float particleLifeTime = 1000;

    bool particleUpdateQued = false;
    ParticleSystem billboardPS;
    AsteroidManager.AsteroidData asteroidData;
    int atlasIndex;

    List<Vector3> asteroidPositions = new List<Vector3>();
    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[maxActiveAsteroids];

    public void Initialize (AsteroidManager.AsteroidData asteroidData, int atlasIndex) {
        // get references
        billboardPS = GetComponent<ParticleSystem>();

        this.asteroidData = asteroidData;
        this.atlasIndex = atlasIndex;

        // initialize the particle system settings
        ParticleSystem.MainModule main = billboardPS.main;
        main.maxParticles = maxActiveAsteroids;
        main.startLifetime = particleLifeTime;

        ParticleSystem.TextureSheetAnimationModule textureSheet = billboardPS.textureSheetAnimation;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.rowMode = ParticleSystemAnimationRowMode.Custom;
        textureSheet.rowIndex = atlasIndex;

        if (AsteroidManager.instance.squareAtlas) {
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.numTilesX = AsteroidManager.altasSideSize;
            textureSheet.numTilesY = AsteroidManager.altasSideSize;
        } else {
            textureSheet.animation = ParticleSystemAnimationType.SingleRow;
            textureSheet.numTilesX = 1;
            textureSheet.numTilesY = AsteroidManager.altasSideSize;
        }
        //textureSheet.frameOverTimeMultiplier = AsteroidManager.altasTotalSize;
    }

    public void PushBack (Vector3 position) {
        asteroidPositions.Add(position);
        QueParticleUpdate();
    }
    public void PopFront () {
        asteroidPositions.RemoveAt(0);
        QueParticleUpdate();
    }

    void QueParticleUpdate() {
        if (particleUpdateQued) return;
        StartCoroutine(UpdateParticlesNextFrame());

    }
    IEnumerator UpdateParticlesNextFrame() {
        particleUpdateQued = true;
        yield return null;
        UpdateParticles();
        particleUpdateQued = false;
    }

    void UpdateParticles() {
        int diff = asteroidPositions.Count - billboardPS.particleCount;
        if (diff > 0) {
            billboardPS.Emit(diff);
        } else if (diff < 0) {
            // the diff should never be less than 0 so ignore

            // remove particles from the front of the array
            //for (int j = 0; j < -diff; j++) {
            //    particles[j].remainingLifetime = -1;
            //}
            //billboardPS.SetParticles(particles);
        }

        billboardPS.GetParticles(particles);

        int i = 0;
        foreach (var asteroidPos in asteroidPositions) {
            particles[i].remainingLifetime = particleLifeTime;
            particles[i].position = asteroidPos;
            particles[i].startSize = asteroidData.squareSize;
            particles[i].startColor = Color.white;

            i++;
        }

        billboardPS.SetParticles(particles, asteroidPositions.Count);
    }
}
