using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMesh : MonoBehaviour
{
    [SerializeField] GameObject pathRoot;

    List<Vector3> pathInfo;
    PoseManager poseManager;
    StateController stateController;
    public bool navigating = false;
    bool inPath = false;
    int navigationIndex = -1;
    GameObject userFirst;
    float pathLength = 0;
    float floorHeight = 0;

    public float pathWidth = 1f;

    private float m_StepSize = 0.2f;
    private float m_ResampledStepSize = 0.2f;
    private float m_tension = 0.5f;

    private float m_PathLength;
    private float m_minStepSize = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        pathInfo = new List<Vector3>();
        Test();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Test()
    {
        List<Vector3> tmp = new List<Vector3>();
        tmp.Add(new Vector3(-8.42f, 20.41f, -5.8f));
        tmp.Add(new Vector3(-10.19f, 20.35f, -5.8f));
        tmp.Add(new Vector3(-10.15f, 13.54f, -5.8f));
        //tmp.Add(new Vector3(3.69f, -21.25f, -5.8f));
        CreateAndShowNavigationPath(new Pose(), tmp, pathRoot);
    }

    void ClearPath(){
        pathRoot.GetComponent<MeshFilter>().mesh.Clear();
    }

    // void ClearPath()
    // {
    //     //navigationIndex = -1;
    //     //inPath = false;
    //     //pathLength = 0;
    //     foreach (Transform item in pathRoot.transform)
    //     {
    //         if (item.gameObject.name != "first")
    //             Destroy(item.gameObject);
    //     }
    // }

    Mesh GetPathMesh(int index, Pose userPose, List<Vector3> path, GameObject pathRoot){
        // Debug.Log($"userPose {userPose.position}");
        
        List<Vector3> points = new List<Vector3>();
        points.Add(userPose.position);
        for(int i = index; i < path.Count; i++)
        {
            points.Add(new Pose(path[i], new Quaternion(0, 0, 0, 1)).position);
        }
        //Debug.DrawRay(poseManager.mapRoot.transform.position, pathRoot.transform.forward * 40f, Color.green, 1000f);
        Debug.Log($"pathRoot.transform.up {pathRoot.transform.forward}");
        Mesh pathMesh = GeneratePath(points, pathRoot.transform.forward);
        return pathMesh;
    }

    // void ShowPath(int index)
    // {
    //     //ClearPath();
    //     //Debug.Log($"Show Path start at {index}/{pathInfo.Count}");

    //     Vector3 s, e;
    //     Pose userPose = poseManager.GetUserPose();
    //     Debug.Log($"userPose {userPose.position}");
    //     userPose.position[2] = floorHeight;
    //     userPose = poseManager.Pose_Map2Global(userPose);
    //     //s = new Vector3(userPos[0], userPos[1], pathInfo[index][2]);
    //     s = userPose.position;
    //     e = poseManager.Pose_Map2Global(new Pose(pathInfo[index], new Quaternion(0, 0, 0, 1))).position;

    //     float length = (e - s).magnitude;

    //     LineRenderer lineRenderer = userFirst.GetComponent<LineRenderer>();
    //     lineRenderer.SetPosition(0, s);
    //     lineRenderer.SetPosition(1, e);

    //     // if(navigationIndex != index)
    //     // {
    //     ClearPath();
    //     navigationIndex = index;
    //     pathLength = 0;
    //     for (int i = index; i < pathInfo.Count - 1; i++)
    //     {
    //         s = poseManager.Pose_Map2Global(new Pose(pathInfo[i], new Quaternion(0, 0, 0, 1))).position;
    //         e = poseManager.Pose_Map2Global(new Pose(pathInfo[i + 1], new Quaternion(0, 0, 0, 1))).position;
    //         float dis_tmp = (e - s).magnitude;
    //         pathLength += dis_tmp;
    //         var p = Instantiate(pathPrefab, pathRoot.transform);
    //         lineRenderer = p.GetComponent<LineRenderer>();
    //         lineRenderer.SetPosition(0, s);
    //         lineRenderer.SetPosition(1, e);
    //         pathes.Add(p);
    //     }
    //     // }
    //     lengthInfo.text = $"Left: {length + pathLength:F2} M";

    // }

    public int CreateAndShowNavigationPath(Pose userPose, List<Vector3> path, GameObject pathRoot)
    {
        // Debug.Log($"CreateAndShowPath: {userPose.position}, {path.Count}");
        MeshFilter m_MeshFilter = pathRoot.GetComponent<MeshFilter>();
        Mesh mesh = m_MeshFilter.mesh;
        mesh.Clear();
        int index = GetUserInPathIndex(userPose, path);
        if (index < 0)
        {
            return index;
        }
        if (inPath == false)
        {
            mesh = GetPathMesh(index, userPose, path, pathRoot);
        }
        else
        {
            mesh = GetPathMesh(index+1, userPose, path, pathRoot);
        }
        m_MeshFilter.mesh = mesh;
        return index;
    }

    int GetUserInPathIndex(Pose userPose, List<Vector3> path)
    {
        // 循环计算到每条路线的距离，距离有小于两米的话则inpath
        int index1 = 0, index2 = 1;
        // Debug.Log($"userPose {userPose.position}");
        userPose.position[2] = floorHeight;
        Vector3 userPosition = userPose.position;

        float minDistance = Vector3.Distance(userPosition, path[index1]);
        for (int i = 1; i < path.Count - 1; i++)
        {
            float distance = Vector3.Distance(userPosition, path[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                index1 = i;
                index2 = i + 1;
            }
        }
        float t = Mathf.InverseLerp(0, Vector3.Distance(path[index1], path[index2]), minDistance);
        Vector3 userOnPath = Vector3.Lerp(path[index1], path[index2], t);

        Vector3 tmpUser = userPosition;
        tmpUser[1] = userOnPath[1];//将高度设为和地图一致
        float distanceToPath = Vector3.Distance(tmpUser, userOnPath);
        float distanceZ = userPosition[1] - userOnPath[1];
        // if (distanceToPath < 1f && distanceZ < 2f)
        // {
        //     inPath = true;
        // }
        // else
        // {
        //     inPath = false;
        // }
        //判断是否到达终点
        if (index2 == path.Count - 1 && Vector3.Distance(tmpUser, path[index2]) <= 1f)
        {
            // StopNavigation();
            return -1;
        }
        // 返回最近距离的点
        return index1;
    }

    // void ShowNode()
    // {
    //     for (int i = 0; i < this.pathType.Count; i++)
    //     {
    //         switch (this.pathType[i])
    //         {
    //             case NodeType.dst:
    //                 // show dst label
    //                 break;
    //             case NodeType.lift:
    //                 // show take lift label
    //                 break;
    //             case NodeType.staircase:
    //                 // show care of staircase
    //                 break;
    //         }
    //     }
    // }

    public void Startnavigation(List<Vector3> path)
    {
        // if (navigating == true)
        // {
        //     StopNavigation();
        // }
        // pathType = nodeType;
        this.pathInfo.Clear();
        if (path.Count == 0)
        {
            Debug.Log("No path");
            return;
        }

        floorHeight = path[0][2];

        for (int i = 0; i < path.Count; i++)
        {
            this.pathInfo.Add(poseManager.Pose_Right2Left(new Pose(path[i], new Quaternion(0, 0, 0, 1))).position); //成为了unity世界坐标
        }
        navigating = true;
    }

    public void StopNavigation()
    {
        navigating = false;
        ClearPath();
        Debug.Log("Navigation stop");
    }

    public void SendStopNavigation()
    {
        Network network = gameObject.GetComponent<Network>();
        network.SendText("", Network.PackageType.navRequest);
    }

    private Mesh GenerateMesh(List<Vector3> points, Vector3 y)
    {
        // Debug.Log($"GenerateMesh: {points.Count}");
        List<Matrix4x4> matrices = new List<Matrix4x4>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 z, x = new Vector3();

            // last point
            if (i == points.Count - 1)
            {
                z = (points[i] - points[i - 1]).normalized;
            }
            else
            {
                z = (points[i + 1] - points[i]).normalized;
            }

            x = Vector3.Cross(y, z);
            y = Vector3.Cross(z, x);

            Quaternion q = Quaternion.LookRotation(z, -y);
            Matrix4x4 m = Matrix4x4.TRS(points[i], q, new Vector3(1f, 1f, 1f));

            matrices.Add(m);

            Vector3 globalPoint = pathRoot.transform.TransformPoint(points[i]);

            Debug.DrawRay(globalPoint, x * 0.4f, Color.red);
            Debug.DrawRay(globalPoint, y * 0.4f, Color.green);
            Debug.DrawRay(globalPoint, z * 0.4f, Color.blue);
        }

        Vector3[] shape = new Vector3[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f) };
        float[] shapeU = new float[] { 0f, 1f };

        int vertsInShape = shape.Length;
        int segments = points.Count - 1;
        int edgeLoops = points.Count;
        int vertCount = vertsInShape * edgeLoops;
        int triCount = shape.Length * segments;
        int triIndexCount = triCount * 3;

        int[] triIndices = new int[triIndexCount];
        int[] lines = new int[] { 0, 1 };

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        for (int i = 0; i < points.Count; i++)
        {
            int offset = i * vertsInShape;

            for (int j = 0; j < vertsInShape; j++)
            {
                int id = offset + j;
                vertices[id] = matrices[i].MultiplyPoint(shape[j] * pathWidth);
                uvs[id] = new Vector2(i / (float)edgeLoops * m_PathLength, shapeU[j]);
            }
        }

        int ti = 0;
        for (int i = 0; i < segments; i++)
        {
            int offset = i * vertsInShape;

            for (int l = 0; l < lines.Length; l += 2)
            {
                int a = offset + lines[l] + vertsInShape;
                int b = offset + lines[l];
                int c = offset + lines[l + 1];
                int d = offset + lines[l + 1] + vertsInShape;

                triIndices[ti] = a; ti++;
                triIndices[ti] = b; ti++;
                triIndices[ti] = c; ti++;
                triIndices[ti] = c; ti++;
                triIndices[ti] = d; ti++;
                triIndices[ti] = a; ti++;
            }
        }
        Mesh m_Mesh = new Mesh();
        m_Mesh.vertices = vertices;
        m_Mesh.triangles = triIndices;
        m_Mesh.uv = uvs;
        return m_Mesh;
    }


    public Mesh GeneratePath(List<Vector3> points, Vector3 up)
    {
        if (points.Count == 2)
        {
            points.Insert(1, Vector3.Lerp(points[0], points[1], 0.333f));
            points.Insert(1, Vector3.Lerp(points[0], points[1], 0.666f));
        }
        List<Vector3> curvePoints = CatmullRomCurvePoints(points);
        // log all points in curvePoints

        curvePoints = ResampleCurve(curvePoints);

        return GenerateMesh(curvePoints, up);
    }

    private float GetT(float t, Vector3 p0, Vector3 p1)
    {
        float a = Mathf.Pow((p1.x - p0.x), 2.0f) + Mathf.Pow((p1.y - p0.y), 2.0f) + Mathf.Pow((p1.z - p0.z), 2.0f);
        float b = Mathf.Pow(a, 0.5f);
        float c = Mathf.Pow(b, m_tension);
        return (c + t);
    }

    private List<Vector3> CatmullRomCurveSegmentPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float segmentDivisions)
    {
        List<Vector3> points = new List<Vector3>();

        float t0 = 1.0f;
        float t1 = GetT(t0, p0, p1);
        float t2 = GetT(t1, p1, p2);
        float t3 = GetT(t2, p2, p3);

        for (float t = t1; t < t2; t += (t2 - t1) / segmentDivisions)
        {
            Vector3 a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector3 a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector3 a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

            Vector3 b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
            Vector3 b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;

            Vector3 c = (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;

            points.Add(c);
        }

        return points;
    }

    private List<Vector3> CatmullRomCurvePoints(List<Vector3> in_points)
    {
        List<Vector3> out_points = new List<Vector3>();

        for (int i = 0; i < in_points.Count - 1; i++)
        {
            Vector3 p0;
            Vector3 p1;
            Vector3 p2;
            Vector3 p3;

            if (i == 0)
            {
                p0 = in_points[i] + (in_points[i] - in_points[i + 1]);
            }
            else
            {
                p0 = in_points[i - 1];
            }

            p1 = in_points[i];
            p2 = in_points[i + 1];

            if (i > in_points.Count - 3)
            {
                p3 = in_points[i] + (in_points[i] - in_points[i - 1]);
            }
            else
            {
                p3 = in_points[i + 2];
            }

            float segmentDivisions = Mathf.Ceil((p2 - p1).magnitude / Mathf.Max(m_minStepSize, m_StepSize));

            List<Vector3> segmentPoints = CatmullRomCurveSegmentPoints(p0, p1, p2, p3, segmentDivisions);
            out_points.AddRange(segmentPoints);
        }

        out_points.Add(in_points[in_points.Count - 1]);

        return out_points;
    }

    private List<Vector3> ResampleCurve(List<Vector3> oldPointPositions)
    {
        List<Vector3> points = new List<Vector3>();

        List<float> distanceAlongOldCurve = new List<float>();
        List<float> relativePositionOnOldCurve = new List<float>();
        List<float> relativePositionOnNewCurve = new List<float>();
        List<int> indexOfFirstPoint = new List<int>();
        List<float> blendBetweenPoints = new List<float>();
        float totalCurveLength = 0.0f;
        int oldPointCount = oldPointPositions.Count;

        //calculate distance along curve and total distance
        for (int i = 0; i < oldPointCount; i++)
        {
            float d;

            if (i == 0)
            {
                d = 0f;
            }
            else
            {
                Vector3 a = oldPointPositions[i - 1];
                Vector3 b = oldPointPositions[i];
                d = (b - a).magnitude;
            }

            totalCurveLength += d;
            distanceAlongOldCurve.Add(totalCurveLength);
        }

        m_PathLength = totalCurveLength;

        //calculate relative position on curve based on distance
        for (int i = 0; i < oldPointCount; i++)
        {
            float rp;
            if (i == 0)
            {
                rp = 0f;
            }
            else
            {
                rp = distanceAlongOldCurve[i] / totalCurveLength;
            }

            relativePositionOnOldCurve.Add(rp);
        }

        //calculate how many new points are needed
        int newPointCount = (int)Mathf.Ceil(totalCurveLength / Mathf.Max(m_minStepSize, m_ResampledStepSize));

        //find first old point further than the new one
        for (int i = 0; i < newPointCount; i++)
        {
            //new point relative position on new curve
            float t = (float)i / (float)(newPointCount - 1.0f);
            relativePositionOnNewCurve.Add(t);

            int k = 0;
            float j = relativePositionOnOldCurve[k];

            while (j < t)
            {
                j = relativePositionOnOldCurve[k];
                if (j <= t)
                {
                    k++;
                }
            }

            indexOfFirstPoint.Add(Mathf.Min(oldPointCount - 1, k));
        }

        for (int i = 0; i < newPointCount; i++)
        {
            int lower = Mathf.Max(indexOfFirstPoint[i] - 1, 0);
            int upper = indexOfFirstPoint[i];
            Vector3 a = oldPointPositions[lower];
            Vector3 b = oldPointPositions[upper];

            float d0 = relativePositionOnOldCurve[lower];
            float d1 = relativePositionOnOldCurve[upper];
            float blend;

            if (d1 - d0 > 0f)
            {
                blend = (relativePositionOnNewCurve[i] - d0) / (d1 - d0);
            }
            else
            {
                blend = 0f;
            }

            Vector3 p = Vector3.Lerp(a, b, blend);

            points.Add(p);
        }

        return points;
    }
}
