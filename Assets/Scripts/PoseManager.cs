using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.ARFoundation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class PoseManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] TMP_Text info;
    [SerializeField] GameObject placedPrefab;
    [SerializeField] ARSessionOrigin mARSessionOrigin;
    [SerializeField] GameObject axes;
    [SerializeField] TMP_Text textInfo;

    public GameObject pathRoot;
    public GameObject mapRoot;

    List<Pose> cvPoses;
    List<Pose> ARPoses;
    List<Vector3> points;
    DateTime locTime;
    DateTime lastUpdateTime;
    float lastSetFloor_z;
    private const float REQUEST_INTERVAL = 0.5f; // 本地化请求间隔时间
    private const int MAX_REQUEST_SKIP_TIME = 30;
    private const float NEED_RELOC_INTERVAL = 10;
    private const float UPDATE_POSE_TO_SERVER_INTERVAL = 2;
    private const float FLOOR_HEIGHT = 4.35f;
    DateTime lastRequestTime;
    Texture2D m_CameraTexture;
    int skipTime = 0;
    PathManager pathManager;
    GameObject local2global;
    StateController stateController;
    ImageAccesser imageAccesser;
    Network network;
    NotificationManager notificationManager;
    //transfer params
    Vector3 map_v;
    Quaternion map_r;
    Pose lastFrameARPose;
    public AddCamPoseStateEnum AddCamPoseState;
    public int userCurrentFloor;
    public int num_inliers = 50;
    public bool locRequesting = false;
    public Pose userPose; //地图坐标系下的用户位置
    AddCamPoseStateEnum lastAddCamPoseState;
    public enum AddCamPoseStateEnum
    {
        WaitingFirst,
        WaitingSecond,
        Normal,
        Abort
    }

    public void SetPlacedPrefab(GameObject prefab)
    {
        placedPrefab = prefab;
    }

    void Start()
    {
        cvPoses = new List<Pose>();
        ARPoses = new List<Pose>();
        points = new List<Vector3>();
        local2global = new GameObject();
        pathManager = gameObject.GetComponent<PathManager>();
        stateController = FindObjectOfType<StateController>();
        imageAccesser = FindObjectOfType<ImageAccesser>();
        network = FindObjectOfType<Network>();
        notificationManager = FindObjectOfType<NotificationManager>();

        lastUpdateTime = DateTime.Now;
        userCurrentFloor = 1;
        if (Application.isEditor)
            Invoke("Test", 2f);
        StartCoroutine(LocalizeRequestCoroutine());
        StartCoroutine(UpdatePoseToServer());
    }

    // Update is called once per frame
    void Update()
    {
        CheckARPoseDrifted();
        UpdateUserPose();
        AutoChangeFloor();
        info.text = mARSessionOrigin.camera.transform.position.ToString();
        info.text += "\n" + userPose.position.ToString();
    }

    void CheckARPoseDrifted()
    {
        Vector3 lpos = mARSessionOrigin.camera.transform.position;
        Quaternion lq = mARSessionOrigin.camera.transform.rotation;
        Pose currentARPose = new Pose(lpos, lq);

        float dis = Vector3.Distance(currentARPose.position, lastFrameARPose.position);
        float angle = Quaternion.Angle(currentARPose.rotation, lastFrameARPose.rotation);
        // Debug.Log($"ARPose diff from last frame: dis {dis} angle {angle}");
        if (dis > 2 || angle > 45)
        {
            Debug.Log("drifte detected");
            // 发生了漂移，定位失效
            if (locRequesting)
            {
                if (AddCamPoseState != AddCamPoseStateEnum.Abort)
                {
                    // 只有不为Abort状态时才记录
                    lastAddCamPoseState = AddCamPoseState;
                }
                AddCamPoseState = AddCamPoseStateEnum.Abort;
                notificationManager.CallNotification(CreateNotification.NotificationType.Warning, "检测到漂移\n定位无效", 1f);
            }
        }
        lastFrameARPose = currentARPose;
    }

    void AutoChangeFloor()
    {
        float z = userPose.position[2];
        float dis_z = z - lastSetFloor_z;
        // Debug.Log(dis_z);
        if (dis_z >= FLOOR_HEIGHT - 0.5 && userCurrentFloor < 4)
        {
            userCurrentFloor = userCurrentFloor == -1 ? 1 : userCurrentFloor + 1;
            SetFloor(userCurrentFloor);
        }
        else if (dis_z <= -FLOOR_HEIGHT + 0.5 && userCurrentFloor > 0)
        {
            userCurrentFloor = userCurrentFloor == 1 ? -1 : userCurrentFloor - 1;
            SetFloor(userCurrentFloor);
        }
    }

    IEnumerator UpdatePoseToServer()
    {
        while (true)
        {
            // 每5秒尝试发一次就好
            if ((DateTime.Now - lastUpdateTime) >= TimeSpan.FromSeconds(UPDATE_POSE_TO_SERVER_INTERVAL))
            {
                SendPose();
                lastUpdateTime = DateTime.Now;
            }
            yield return new WaitForSeconds(REQUEST_INTERVAL);
        }
    }


    public void Test()
    {
        AddARPose();
        AddCamPose(new Pose(new Vector3(-9.326386156820048f,
            -12.610138008522329f,
            -18.026988514331386f), new Quaternion(
            -0.019712626977367705f,
            0.7727931220865669f,
            -0.6336685179250837f,
            -0.02943488039683533f)), 100);
        // AddARPose();
        // AddCamPose(new Pose(new Vector3(-8.363897154033015f,
        //     -16.381751362318358f,
        //     1.6170759029187733f), new Quaternion( -0.00030173603481409235f,
        //     -0.5759417383146318f,
        //     0.8157736579860065f, 0.05295622681625217f)), 100);
        // UpdateUserPose();

        SetFloor(-1);
        // AddCamPose(new Pose(new Vector3( -10.736507691339815f, -8.023416617344726f, 13.946564679182412f), new Quaternion(0.006318779991281585f, 0.7977390758376688f, -0.6027204426853294f, -0.01733516282831502f)));
    }

    public void UpdateUserPose()
    {
        //用户在地图中的坐标实时更新
        Vector3 gPos = mARSessionOrigin.camera.transform.position;
        Quaternion gq = mARSessionOrigin.camera.transform.rotation;
        Pose camera_globalPose = new(gPos, gq);
        userPose = Pose_Global2Map(camera_globalPose);
    }

    public void SetFloor(int floor)
    {
        Debug.Log($"set floor {floor}");
        userCurrentFloor = floor;
        MiniMapManager miniMapManager = FindObjectOfType<MiniMapManager>();
        // create a list with given value
        miniMapManager.SetMiniMapFloor(floor);
        lastSetFloor_z = userPose.position[2];
    }

    public void AddCamPose(Pose rightp, int inliers)
    {
        this.num_inliers = inliers;
        Debug.Log("add cam pose");
        locTime = DateTime.Now;
        //the localization's result
        //the pose of virtual camera in the map 
        Pose leftp = Pose_Right2Left(rightp);

        //update params which are used to transfer map coor to local coor
        map_v = leftp.position;
        map_r = leftp.rotation;
        map_r.Normalize();

        Pose cameraPose = GetCameraPoseInMap();
        cvPoses.Add(cameraPose);
        if (cvPoses.Count > 1000)
        {
            Debug.Log("cv poses remove");
            cvPoses.RemoveRange(0, 500);
        }

        // 在这里实现一个通过两次定位，确定位置可靠性的逻辑代码
        switch (AddCamPoseState)
        {
            case AddCamPoseStateEnum.WaitingFirst:
                AddCamPoseState = AddCamPoseStateEnum.WaitingSecond;
                break;
            case AddCamPoseStateEnum.WaitingSecond:
                // 判断逻辑
                if (IsTwoLocateResValid())
                {
                    AddCamPoseState = AddCamPoseStateEnum.Normal;
                    stateController.LocSystemStatus_Ready();
                    UpdateMapRoot();
                    notificationManager.CallNotification(CreateNotification.NotificationType.Success, "定位成功", 0.7f);
                }
                else
                {
                    AddCamPoseState = AddCamPoseStateEnum.WaitingFirst;
                    notificationManager.CallNotification(CreateNotification.NotificationType.Warning, "两次定位差距过大，失败\n请继续保持手机平稳", 1f);
                }
                break;
            case AddCamPoseStateEnum.Normal:
                stateController.LocSystemStatus_Ready();
                UpdateMapRoot();
                notificationManager.CallNotification(CreateNotification.NotificationType.Success, "定位成功", 0.7f);
                break;
            case AddCamPoseStateEnum.Abort:
                // recover
                AddCamPoseState = lastAddCamPoseState;
                RemoveLastARPose();
                break;
        }
    }

    bool IsTwoLocateResValid()
    {
        Pose res1 = cvPoses[^1];
        Pose res2 = cvPoses[^2];
        Pose ar1 = ARPoses[^1];
        Pose ar2 = ARPoses[^2];
        // 判断两组各自的相对位移和旋转角度
        float dis = Vector3.Distance(res1.position, res2.position);
        float angle = Quaternion.Angle(res1.rotation, res2.rotation);
        float ar_dis = Vector3.Distance(ar1.position, ar2.position);
        float ar_angle = Quaternion.Angle(ar1.rotation, ar2.rotation);
        Debug.Log($"dis {dis} angle {angle} ar_dis {ar_dis} ar_angle {ar_angle}");
        // 判断距离差和角度差是否在阈值内
        float abs_dis = Mathf.Abs(dis - ar_dis);
        float abs_angle = Mathf.Abs(angle - ar_angle);
        Debug.Log($"abs_dis {abs_dis} abs_angle {abs_angle}");
        if (abs_dis < 0.5 && abs_angle < 5)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddARPose()
    {
        //the ar camera's pose
        Vector3 lpos = mARSessionOrigin.camera.transform.position;
        Quaternion lq = mARSessionOrigin.camera.transform.rotation;
        Pose p = new Pose(lpos, lq);
        ARPoses.Add(p);
        if (ARPoses.Count > 1000)
        {
            Debug.Log("AR poses remove");
            ARPoses.RemoveRange(0, 500);
        }
    }

    public void RemoveLastARPose()
    {
        if (ARPoses.Count > 0)
        {
            ARPoses.RemoveAt(ARPoses.Count - 1);
        }
    }

    public Pose Pose_Right2Left(Pose p)
    {
        Vector3 rt = p.position;
        Quaternion rq = p.rotation;
        Pose ret = new Pose(new Vector3(rt[0], -rt[1], rt[2]), new Quaternion(-rq[0], rq[1], -rq[2], rq[3]));
        return ret;
    }

    public Pose Pose_Left2Right(Pose p)
    {
        Vector3 lt = p.position;
        Quaternion lq = p.rotation;
        Pose ret = new Pose(new Vector3(lt[0], -lt[1], lt[2]), new Quaternion(-lq[0], lq[1], -lq[2], lq[3]));
        return ret;
    }

    public Pose Pose_Map2Cam(Pose p)
    {
        //从地图坐标转到相机坐标
        Pose newLocPose = new Pose(map_r * p.position + map_v, map_r * p.rotation);
        return newLocPose;
    }

    public Pose Pose_Local2Global(Pose local)
    {
        //局部坐标转全局坐标
        Pose lastARPose = ARPoses[^1];
        local2global.transform.SetPositionAndRotation(lastARPose.position, lastARPose.rotation);
        Pose global_test = local2global.transform.TransformPose(local);
        return global_test;
    }

    public Pose Pose_Map2Global(Pose mapPose)
    {
        return Pose_Local2Global(Pose_Map2Cam(mapPose));
    }

    public Pose Pose_Global2Map(Pose globalPose)
    {
        Pose mapPose = mapRoot.transform.InverseTransformPose(globalPose);
        return mapPose;
    }

    public Pose GetCameraPoseInMap()
    {
        Vector3 cam_origin = new Vector3(0, 0, 0);
        Matrix4x4 R = Matrix4x4.Rotate(map_r);
        Matrix4x4 R_inv = R.inverse;
        Vector3 cam_global = R_inv.MultiplyVector(cam_origin - map_v);
        //TODO 计算相机的角度
        Quaternion cam_q = Quaternion.Inverse(map_r);
        // print(cam_global);
        return new Pose(cam_global, cam_q);
    }

    public Pose GetARCameraPoseInGlobal()
    {
        Vector3 lpos = mARSessionOrigin.camera.transform.position;
        Quaternion lq = mARSessionOrigin.camera.transform.rotation;
        return new Pose(lpos, lq);
    }

    public Pose GetUserPose()
    {
        return userPose;
    }

    void UpdateARCameraPose()
    {
        //通过上一次对应的AR相机pose和定位得到的相机pose，计算两者的差值，并将其应用在ARSessionOrigin上，使得ARSessionOrigin中的ARCamera上一次的pose与相机的pose一致
        Pose lastARPose = ARPoses[^1];
        Pose lastCVPose = cvPoses[^1];
        lastCVPose.rotation.Normalize();

        Matrix4x4 ARCamera = Matrix4x4.TRS(lastARPose.position, lastARPose.rotation, new Vector3(0, 0, 0));

        Debug.Log($"lastAR {lastARPose.rotation} laseCV {lastCVPose.rotation} sessionOrigin {mARSessionOrigin.transform.rotation}");
        Debug.Log($"lastAR {lastARPose.position} laseCV {lastCVPose.position} sessionOrigin {mARSessionOrigin.transform.position}");
        mARSessionOrigin.transform.position += lastCVPose.position - lastARPose.position;
        mARSessionOrigin.transform.rotation *= Quaternion.Inverse(lastARPose.rotation) * lastCVPose.rotation;

    }

    void UpdateMapRoot()
    {
        Pose origin = new(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1));
        Pose global = Pose_Map2Global(origin);
        Debug.Log($"global map origin {global.position} {global.rotation}");
        mapRoot.transform.SetPositionAndRotation(global.position, global.rotation);
    }


    public void SendPose()
    {
        if (stateController.networkStatus != StateController.NetworkStatus.Connected)
        {
            Debug.Log("Network not connected, skip send pose");
            return;
        }
        Pose cameraPose = userPose;
        Pose cameraPose_right = Pose_Left2Right(cameraPose);
        float[] tvec = new float[] { cameraPose_right.position[0], cameraPose_right.position[1], cameraPose_right.position[2] };
        float[] qvec = new float[] { cameraPose_right.rotation.w, cameraPose_right.rotation.x, cameraPose_right.rotation.y, cameraPose_right.rotation.z };

        JObject Jpose = new();
        Jpose.Add("tvec", JToken.FromObject(tvec));
        Jpose.Add("qvec", JToken.FromObject(qvec));
        JObject updatePoseMsg = new();
        updatePoseMsg.Add("Pose", Jpose);
        updatePoseMsg.Add("LocStatus", ((int)stateController.locSystemStatus));
        updatePoseMsg.Add("Floor", userCurrentFloor);
        string json = JsonConvert.SerializeObject(updatePoseMsg);
        network.SendText(json, Network.PackageType.updatePose);
    }

    private IEnumerator LocalizeRequestCoroutine()
    {
        // 在这里实现视觉定位的逻辑，何时请求定位
        while (true)
        {
            // 如果上一个定位未回复，则不定位
            if (locRequesting == true)
            {
                Debug.Log($"Last request not replied, skip {skipTime}");
                skipTime += 1;
                if (skipTime > MAX_REQUEST_SKIP_TIME)
                {
                    // 相当于MAX_REQUEST_SKIP_TIME*REQUEST_INTERVAL时间没收到回复，则重置
                    // 重启定位系统
                    Debug.Log("Locate failed, restart loc");
                    skipTime = 0;
                    locRequesting = false;
                }
            }
            else
            {
                skipTime = 0;
                switch (stateController.locSystemStatus)
                {
                    case StateController.LocSystemStatus.Uninitialized:
                        //始终尝试定位
                        TryRequestLocate();
                        break;
                    case StateController.LocSystemStatus.Ready:
                        //不需要定位
                        break;
                    case StateController.LocSystemStatus.NeedRelocation:
                        //每隔几秒尝试定位
                        if ((DateTime.Now - lastRequestTime) >= TimeSpan.FromSeconds(NEED_RELOC_INTERVAL))
                        {
                            TryRequestLocate();
                        }
                        break;
                    case StateController.LocSystemStatus.LocationExpired:
                        //始终尝试定位
                        TryRequestLocate();
                        break;
                }
            }
            yield return new WaitForSeconds(REQUEST_INTERVAL); //每0.5秒钟执行一次循环
        }
    }

    public void TryRequestLocate()
    {
        if (stateController.networkStatus != StateController.NetworkStatus.Connected)
        {
            Debug.Log("Network not connected, skip send image");
            // network.ConnectBtnDown();
            return;
        }
        try
        {
            m_CameraTexture = imageAccesser.GetCameraFrame();
            AddARPose();

            byte[] picDataJPG = m_CameraTexture.EncodeToJPG();

            // 上面生成的是什么东西？

            Debug.Log($"length {picDataJPG.Length}");

            network.SendByte(picDataJPG, Network.PackageType.locRequest, picDataJPG.Length);
            Debug.Log("Send locate request");
            locRequesting = true;
            // show the time of send
            textInfo.text = "Send loc request at " + DateTime.Now.ToString("HH:mm:ss");
            lastRequestTime = DateTime.Now;
        }
        catch (Exception e)
        {
            Debug.Log($"Send image failed {e}");
        }
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public float GetDistanceFromLastLocPosition()
    {
        Pose lastLocPosition = cvPoses[^1];
        Pose nowPosition = GetUserPose();
        float distance = Vector3.Distance(lastLocPosition.position, nowPosition.position);
        return distance;
    }

    public void OnLocLogToggle()
    {
        bool isActive = textInfo.gameObject.activeSelf;
        textInfo.gameObject.SetActive(!isActive);
        info.gameObject.SetActive(!isActive);
    }

}
