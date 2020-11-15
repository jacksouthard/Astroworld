using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneCamera : MonoBehaviour
{
    public float baseSpeed = 20;
    public float forwardSpeedMul = 1;
    const float blend = 4;
    Vector3 momentum;

    private void Start() {
        momentum = Vector3.zero;
    }

    private void Update() {
        float forward = 0;
        if (Input.GetKey(KeyCode.E)) forward = forwardSpeedMul;
        else if (Input.GetKey(KeyCode.Q)) forward = -forwardSpeedMul;
        Vector3 target = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), forward);
        target *= baseSpeed;

        momentum = Vector3.Lerp(momentum, target, blend * Time.deltaTime);
        transform.Translate(momentum * Time.deltaTime);

        BeltGenerator.instance.SetCurrentZ(transform.position.z);
    }
}
