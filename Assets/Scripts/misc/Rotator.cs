using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float rotationSpeed = 45f; // Degrees per second

    private void Update()
    {
        // Rotate around Y-axis
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
