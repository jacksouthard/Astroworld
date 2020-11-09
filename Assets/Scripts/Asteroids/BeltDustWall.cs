using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltDustWall : MonoBehaviour
{
    Material mat;
    public float z { get; private set; }

    public void Initialize (float z, Vector4 randomOffset) {
        this.z = z;
        mat = GetComponent<MeshRenderer>().material;
        mat.SetVector("_RandomOffsets", randomOffset);
        GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    public void SetVisibility (float visibilityRatio) {
        mat.SetFloat("_Visibility", visibilityRatio);
    }
}
