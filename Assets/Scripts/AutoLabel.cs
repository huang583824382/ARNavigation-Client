using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoLabel : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // Debug.Log(mainCamera.transform.position);
        Quaternion q = Quaternion.LookRotation(transform.position - mainCamera.transform.position );
        transform.rotation = q;
    }
}
