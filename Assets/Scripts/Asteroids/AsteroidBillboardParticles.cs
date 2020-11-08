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

    List<AsteroidManager.AsteroidBillboard> asteroidBillboards = new List<AsteroidManager.AsteroidBillboard>();
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

    public void PushBack (AsteroidManager.AsteroidBillboard billboard) {
        asteroidBillboards.Add(billboard);
        QueParticleUpdate();
    }
    public void PopFront () {
        asteroidBillboards.RemoveAt(0);
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
        int diff = asteroidBillboards.Count - billboardPS.particleCount;
        if (diff > 0) {
            billboardPS.Emit(diff);
        }

        billboardPS.GetParticles(particles);

        int i = 0;
        foreach (var billboard in asteroidBillboards) {
            particles[i].remainingLifetime = particleLifeTime;
            particles[i].position = billboard.position;
            particles[i].startSize = asteroidData.squareSize * billboard.size;
            particles[i].startColor = Color.white;

            i++;
        }

        billboardPS.SetParticles(particles, asteroidBillboards.Count);
    }
}
