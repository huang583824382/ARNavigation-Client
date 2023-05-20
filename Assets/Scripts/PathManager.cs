using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PathManager : MonoBehaviour
{
    [SerializeField] GameObject pathPrefab;
    [SerializeField] GameObject pathRoot;
    [SerializeField] TMP_Text lengthInfo;

    List<Vector3> pathInfo;
    List<NodeType> pathType;
    PoseManager poseManager;
    StateController stateController;
    public bool navigating = false;
    bool inPath = false;
    int navigationIndex = -1;
    GameObject userFirst;
    float pathLength = 0;
    float floorHeight = 0;

    public float pathWidth = 0.5f;

    private float m_StepSize = 5f;
    private float m_ResampledStepSize = 0.2f;
    private float m_tension = 0.5f;

    private float m_PathLength;
    private float m_minStepSize = 0.1f;

    public enum NodeType : short
    {
        corner,
        src,
        dst,
        lift,
        staircase
    }
    void Start()
    {
        pathType = new List<NodeType>();
        pathInfo = new List<Vector3>();
        poseManager = gameObject.GetComponent<PoseManager>();
        stateController = FindObjectOfType<StateController>();
        // lengthInfo = GameObject.Find("PathLengthText").GetComponent<TMP_Text>();

        // Test();
    }

    private void Update()
    {
        // if (navigating == true)
        // {
        //     Pose userPose = poseManager.GetUserPose();
        //     userPose.position[2] = floorHeight;
        //     int res = CreateAndShowNavigationPath(userPose, pathInfo, pathRoot);
        //     if (res < 0)
        //     {
        //         FindObjectOfType<NavigationManager>().StopNavigation();
        //     }
        // }
    }

    public void Test()
    {
        
        var b1p = new Vector3(1f, 0f, 0f);
        var b2p = new Vector3(-1f, 0f, 0f);
        var b1 = Instantiate(pathPrefab, pathRoot.transform);
        b1.transform.localPosition = b1p;

        var b2 = Instantiate(pathPrefab, pathRoot.transform);
        b2.transform.localPosition = b2p;

        var z = new Vector3(1, 0, 0);
        var y = pathRoot.transform.forward;
        var x = Vector3.Cross(y, z).normalized;
        y = Vector3.Cross(z, x).normalized;

        Quaternion q = Quaternion.LookRotation(z, -y);
        Matrix4x4 m = Matrix4x4.TRS(new Vector3(-1, -1, -1), q, new Vector3(1f, 1f, 1f));
        b1p = m.MultiplyPoint(b1p);
        b2p = m.MultiplyPoint(b2p);
        b1.transform.localPosition = b1p;
        b2.transform.localPosition = b2p;

        Debug.DrawRay(b1.transform.position, z, Color.green, 1000f);
        Debug.DrawRay(b1.transform.position, -y, Color.red, 1000f);
        Debug.DrawRay(b1.transform.position, x, Color.blue, 1000f);
        
        var local_x_dir = pathRoot.transform.InverseTransformDirection(x);
        var b3 = Instantiate(pathPrefab, pathRoot.transform);
        var b4 = Instantiate(pathPrefab, pathRoot.transform);
        b3.transform.localPosition = b1p-local_x_dir;
        b4.transform.localPosition = b1p+local_x_dir;
    }

    void ClearPath(){
        pathRoot.GetComponent<MeshFilter>().mesh.Clear();
    }

    Mesh GetPathMesh(int index, Pose userPose, List<Vector3> path, GameObject pathRoot){
        // Debug.Log($"userPose {userPose.position}");
        
        List<Vector3> points = new List<Vector3>();
        points.Add(userPose.position);
        for(int i = index; i < path.Count; i++)
        {
            points.Add(new Pose(path[i], new Quaternion(0, 0, 0, 1)).position);
        }
        //Debug.DrawRay(poseManager.mapRoot.transform.position, pathRoot.transform.forward * 40f, Color.green, 1000f);
        Mesh pathMesh = GeneratePath(points, pathRoot.transform.forward);
        return pathMesh;
    }

    Mesh GetPathMesh(List<Vector3> path, GameObject pathRoot){
        List<Vector3> points = new List<Vector3>();
        for(int i = 0; i < path.Count; i++)
        {
            points.Add(new Pose(path[i], new Quaternion(0, 0, 0, 1)).position);
        }
        Mesh pathMesh = GeneratePath(points, Vector3.forward);
        return pathMesh;
    }

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
        // 根据用户位置来显示路径
        if (inPath == false)
        {
            mesh = GetPathMesh(index, userPose, path, pathRoot);
        }
        else
        {
            mesh = GetPathMesh(index+1, userPose, path, pathRoot);
        }
        // mesh = GetPathMesh(path, pathRoot); // 不根据用户位置来显示路径
        m_MeshFilter.mesh = mesh;
        return index;
    }

    int GetUserInPathIndex(Pose userPose, List<Vector3> path)
    {
        if(path.Count == 1){
            return 0;
        }
        else if(path.Count == 0){
            return -1;
        }
        // 循环计算到每条路线的距离，距离有小于两米的话则inpath
        int index1 = 0, index2 = 1;
        // Debug.Log($"userPose {userPose.position}");
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
        float distanceToPath = Vector3.Distance(tmpUser, userOnPath);
        if (distanceToPath < 1.5f)
        {
            inPath = true;
        }
        else
        {
            inPath = false;
        }
        //判断是否到达终点
        if (index2 == path.Count - 1 && Vector3.Distance(tmpUser, path[index2]) <= 2f)
        {
            // StopNavigation();
            return -1;
        }
        // 返回最近距离的点
        return index1;
    }

    void ShowNode()
    {
        for (int i = 0; i < this.pathType.Count; i++)
        {
            switch (this.pathType[i])
            {
                case NodeType.dst:
                    // show dst label
                    break;
                case NodeType.lift:
                    // show take lift label
                    break;
                case NodeType.staircase:
                    // show care of staircase
                    break;
            }
        }
    }

    private Mesh GenerateMesh(List<Vector3> points, Vector3 y)
    {
        
        // Debug.Log($"GenerateMesh: {points.Count}");
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        List<Vector3> dir = new();
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 globalPoint = pathRoot.transform.TransformPoint(points[i]);
            // Debug.DrawRay(globalPoint, y * 0.4f, Color.yellow);
            
            Vector3 z, x = new Vector3();

            // last point
            if (i == points.Count - 1)
            {
                z = (pathRoot.transform.TransformPoint(points[i]) - pathRoot.transform.TransformPoint(points[i-1])).normalized;
            }
            else
            {
                z = (pathRoot.transform.TransformPoint(points[i+1]) - pathRoot.transform.TransformPoint(points[i])).normalized;
            }

            x = Vector3.Cross(y, z).normalized;
            y = Vector3.Cross(z, x).normalized;
            dir.Add(pathRoot.transform.InverseTransformDirection(x));
            // z = Vector3.Cross(x, y).normalized;

            Quaternion q = Quaternion.LookRotation(z, -y);
            Matrix4x4 m = Matrix4x4.TRS(points[i], q, new Vector3(1f, 1f, 1f));
            

            matrices.Add(m);


            // Debug.DrawRay(globalPoint, tmp * 0.4f, Color.yellow);
            Debug.DrawRay(globalPoint, x * 0.4f, Color.red);
            Debug.DrawRay(globalPoint, -y * 0.4f, Color.green);
            Debug.DrawRay(globalPoint, z * 0.4f, Color.blue);
        }

        Vector3[] shape = new Vector3[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f) };
        // Test
        

        float[] shapeU = new float[] { 0f, 1f };

        int vertsInShape = shape.Length;
        int segments = points.Count - 1;
        // Debug.Log($"segments: {segments}");
        int edgeLoops = points.Count;
        int vertCount = vertsInShape * edgeLoops;
        int triCount = shape.Length * segments;
        // Debug.Log($"triCount: {triCount}");
        long triIndexCount = triCount * 3;
        // Debug.Log($"triIndexCount: {triIndexCount}");

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
                // dir[i] = pathRoot.transform.InverseTransformDirection(dir[i]);
                vertices[id] = points[i]+ dir[i] * 0.5f * Mathf.Pow(-1, j)*pathWidth;
                // vertices[id] = matrices[i].MultiplyPoint(shape[j] * pathWidth);

                // Debug.Log($"vertices[{id}]: {vertices[id]}");
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
        var new_points = InterpolatePoints(points);
        List<Vector3> curvePoints = CatmullRomCurvePoints(new_points);
        // log all points in curvePoints
        // List<Vector3> curvePoints = points;
        // Debug.Log(curvePoints.Count);
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

    List<Vector3> InterpolatePoints(List<Vector3> points) {
        List<Vector3> interpolatedPoints = new List<Vector3>();
        if (points.Count < 2) {
            return points;
        }
        interpolatedPoints.Add(points[0]);
        for (int i = 1; i < points.Count; i++) {
            Vector3 p0 = points[i-1];
            Vector3 p1 = points[i];
            float distance = Vector3.Distance(p0, p1);
            int divisions = Mathf.CeilToInt(distance / m_StepSize);
            float step = 1.0f / divisions;
            for (int j = 1; j < divisions; j++) {
                interpolatedPoints.Add(Vector3.Lerp(p0, p1, j * step));
            }
            interpolatedPoints.Add(p1);
        }
        return interpolatedPoints;
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
