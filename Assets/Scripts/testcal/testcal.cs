using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;

public class testcal : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] GameObject placedPrefab;
    [SerializeField] GameObject cube;
    [SerializeField] Camera cam;
    float s;
    Vector3 subp;
    Vector3 addp;
    Quaternion r;
    GameObject mapRoot;
    TextAsset point3Djson;
    List<Vector3> points;
    void Start()
    {
        Debug.Log("hello");
        //Vector3 v1 = new Vector3(-2.70f, 0.47f, 1.27f);
        //Quaternion q1 = new Quaternion(-0.15778f, 0.43770f, -0.06677f, -0.88264f);

        //Vector3 v2 = new Vector3(-1.33f, 1.29f, -4.19f);
        //Quaternion q2 = new Quaternion(0.98985f, 0.13382f, 0.04311f, -0.02096f);

        //var camera1 = Instantiate(prefab, v1, q1);
        //var camera2 = Instantiate(prefab, v2, q2);
        //cube.transform.position = new Vector3(0.05f, 0.05f, 0.05f);
        //var obj1 = Instantiate(cube, camera2.transform);
        //Debug.Log(obj1.transform.position.ToString());
        //Debug.Log(obj1.transform.localPosition.ToString());
        //Quaternion qr = q1 * Quaternion.Inverse(q2);
        //Vector3 camera2t = qr * (v2 - v2) + v1;
        //camera2.transform.position = camera2t;

        //Vector3 obj1tp = qr * (obj1.transform.position - v2) + v1;
        //Quaternion obj1tr = qr * obj1.transform.rotation;

        //var obj1t = Instantiate(cube, obj1tp, obj1tr);

        //��ȡ����������λ�˵�����
        //var camera = Instantiate(prefab, new Vector3(0.05f, 0, 0), new Quaternion(0, 0, 0, 0));
        //var obj = Instantiate(cube, new Vector3(0.1f, 0.1f, 0.1f), new Quaternion(-0.15778f, 0.43770f, -0.06677f, -0.88264f));

        //var localp = obj.transform.position - camera.transform.position;
        //var localr = obj.transform.rotation * Quaternion.Inverse(camera.transform.rotation);
        //prefab.transform.position = localp;
        //prefab.transform.rotation = localr;
        //Instantiate(prefab, camera.transform);

        // 3Dpoints json
        //TextAsset json = Resources.Load("points3D") as TextAsset;
        //try
        //{
        //    JObject obj = JObject.Parse(json.ToString());
        //    Debug.Log(obj["points"][0].ToString());

        //}catch(Exception e)
        //{
        //    Debug.Log(e.ToString());
        //}
        //points = new List<Vector3>();
        //mapRoot = new GameObject("mapRoot");
        //subp = new Vector3(-0.153f, -4.68f, 3.8932f);
        //addp = Vector3.zero;
        //s = 1;
        //r = new Quaternion(0, 0, 0, 1) * Quaternion.Inverse(new Quaternion( -0.063586f, -0.14206f, 0.14877f, 0.97655f));

        
        
        subp = new Vector3(-0.15307997f, 4.68003314f, 3.89321794f);
        r = new Quaternion(0.14205845f, -0.06358634f, 0.14876632f, 0.97654736f);
        Vector3 res = Vector3.zero - Quaternion.Inverse(r) * subp;
        Debug.Log(res.ToString());

        cam.transform.position = res;
        //cam.transform.rotation = Quaternion.Inverse(r);

        // test3Dpoints();
        ////Done!!!
        //Matrix4x4 rot = new Matrix4x4();
        //rot.SetTRS(new Vector3(0, 0, 0), Quaternion.Inverse(rt), new Vector3(1, 1, 1));
        //res = rot * subp;
        //Debug.Log(res.ToString());
    }
    public Pose right2left(Pose p)
    {
        Debug.Log($"right: {p.position.ToString()}");
        Vector3 rt = p.position;
        Quaternion rq = p.rotation;

        Pose ret = new Pose(new Vector3(rt[0], -rt[1], rt[2]), new Quaternion(-rq[0], rq[1], -rq[2], rq[3]));
        Debug.Log($"left: {ret.position.ToString()}");

        return ret;
    }
    void test3Dpoints()
    {
        point3Djson = Resources.Load("points3D") as TextAsset;
        try
        {
            JObject obj = JObject.Parse(point3Djson.ToString());
            Debug.Log(obj["points"][0].ToString());
            int i = 0;
            foreach (JObject point in obj["points"])
            {
                //Debug.Log(point.ToString());
                float[] t = point["pos"].ToObject<List<float>>().ToArray();
                Vector3 pos = new Vector3(t[0], t[1], t[2]);
                pos = right2left(new Pose(pos, new Quaternion(0, 0, 0, 1))).position;
                //points.Add(pos);
                var p = Instantiate(placedPrefab, cam.transform);
                // transfer
                //Pose tmp = PoseTransfer(new Pose(pos, new Quaternion(0, 0, 0, 0)));
                p.transform.localPosition = r*pos+subp;
                //p.transform.localPosition = pos;
                p.name = i.ToString();
                i++;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void RenewPoints()
    {
        Debug.Log("renew points");
        for (int i = 0; i < mapRoot.transform.childCount; i++)
        {

            Vector3 pos = points[i];
            //Debug.Log(pos.ToString());
            Pose tmp = PoseTransfer(new Pose(pos, new Quaternion(0, 0, 0, 0)));

            mapRoot.transform.GetChild(i).position = tmp.position;
        }
    }

    Pose PoseTransfer(Pose p)
    {
        p.position = r * (p.position - subp) * s + addp;
        p.rotation = r * p.rotation;
        return p;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
