using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotation : MonoBehaviour
{
    public GameObject target;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 t1p = new Vector3(1, 0, 0);
        Vector3 t2p = new Vector3(-1, 0, 0);
        var t1 = Instantiate(target, new Vector3(1, 0, 0), Quaternion.identity);
        var t2 = Instantiate(target, new Vector3(-1, 0, 0), Quaternion.identity);
        Vector3 dir = new Vector3(1, 0, 1);
        Vector3 up = new Vector3(0, 1, 0);

        Quaternion q = Quaternion.LookRotation(dir, -up);
        Debug.DrawRay(Vector3.zero, dir, Color.red, 100f);
        Debug.DrawRay(Vector3.zero, -up, Color.green, 100f);
        Matrix4x4 m = Matrix4x4.TRS(new Vector3(0, 0, 0), q, new Vector3(1f, 1f, 1f));

        t1p = m.MultiplyPoint(t1p);
        t2p = m.MultiplyPoint(t2p);
        t1.transform.position = t1p;
        t2.transform.position = t2p;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
