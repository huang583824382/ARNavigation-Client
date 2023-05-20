using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARChange : MonoBehaviour
{
    ARSessionOrigin arSession;
    // Start is called before the first frame update
    void Start()
    {
        arSession = FindObjectOfType<ARSessionOrigin>();
        Debug.Log(arSession.camera.transform);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void test()
    {
        Debug.Log($"Before: {arSession.camera.transform.position}");
        arSession.transform.SetPositionAndRotation(new Vector3(1f, 1f, 1f), new Quaternion(0, 0, 0, 1));
        Debug.Log($"After: {arSession.camera.transform.position}");

    }
}
