using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class NavigationManager : MonoBehaviour
{
    [SerializeField] private GameObject m_TargetsList = null;
    [SerializeField] private Sprite m_ShowListIcon = null;
    [SerializeField] private Sprite m_SelectTargetIcon = null;
    [SerializeField] private Image m_TargetsListIcon = null;
    [SerializeField] private TextMeshProUGUI m_TargetsListText = null;
    [SerializeField] private GameObject m_StopNavigationButton = null;
    [SerializeField] GameObject pathRoot;
    [SerializeField] GameObject destinationPrefab;

    NotificationManager notificationManager;
    PathManager pathManager;
    PoseManager poseManager;
    StateController stateController;
    AutoLabelController autoLabelController;
    private enum NavigationState { NotNavigating, Navigating };
    private NavigationState m_navigationState = NavigationState.NotNavigating;
    private Dictionary<int, List<string>> places;
    List<Vector3> path;
    private List<int> pathFloor;
    private List<int> pathType;
    private bool navigationStartFlag = false;
    private List<Vector3> navigationStartPath;
    int navigationIndex;
    string destination;
    string toggleText = "显示导航目的地";
    int NavigationDesFloor = 1;
    public List<int> floors;
    List<List<Vector3>> navigationPathes;
    List<int> navigationFloors;
    bool firstUpdate;
    string destinationStr;
    int navigationMode = 0;
    int tmpLabelFloor = 0;
    GameObject desObject;


    // Start is called before the first frame update
    void Start()
    {
        notificationManager = FindObjectOfType<NotificationManager>();
        pathManager = FindObjectOfType<PathManager>();
        poseManager = FindObjectOfType<PoseManager>();
        stateController = FindObjectOfType<StateController>();
        autoLabelController = FindObjectOfType<AutoLabelController>();

        places = new Dictionary<int, List<string>>();
        path = new List<Vector3>();
        // Test();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNavigationUI(m_navigationState);
        lock (this)
        {
            if (navigationStartFlag)
            {
                navigationStartFlag = false;
                StartNavigation(navigationStartPath);
            }
        }
        if (m_navigationState == NavigationState.Navigating)
        {
            // TODO: 需要在这里进行楼层的判断，传入生成路径的是当前的楼层，其他都在这里实现
            List<Vector3> pathInFloor = GetPathInFloor();
            if (pathInFloor.Count <= 1 && navigationIndex >= navigationPathes.Count)
            {
                Debug.Log("up or down is destination");
                StopNavigation();
                return;
            }
            Pose userPose = poseManager.GetUserPose();
            userPose.position[2] = pathInFloor[0][2];
            int res = pathManager.CreateAndShowNavigationPath(userPose, pathInFloor, pathRoot);
            if (firstUpdate)
            {
                // 生成label显示
                firstUpdate = false;
                Vector3 labelPos = pathInFloor[pathInFloor.Count - 1];
                labelPos[2] += 1f;
                if (navigationIndex != navigationPathes.Count - 1)
                {
                    // 如果上面还有楼层，则寻找需要前往的楼层或者最后的楼层
                    tmpLabelFloor = navigationIndex;
                    while (tmpLabelFloor + 1 < navigationPathes.Count && navigationPathes[tmpLabelFloor + 1].Count <= 1)
                    {
                        tmpLabelFloor++;
                    }
                    // 判断楼梯还是电梯
                    if (GetTypeOfEndInFloor(navigationIndex) == 1)
                        autoLabelController.CreateLabel("navigation", labelPos, "请乘坐电梯前往" + navigationFloors[tmpLabelFloor + 1] + "楼");
                    else
                        autoLabelController.CreateLabel("navigation", labelPos, "请从楼梯前往" + navigationFloors[tmpLabelFloor + 1] + "楼");
                }
                else
                {
                    // 没有其他楼层
                    // 更改显示为目的地标志
                    autoLabelController.CreateLabel("navigation", labelPos, destinationStr);
                    desObject = Instantiate(destinationPrefab, pathRoot.transform);
                    Vector3 desPos = pathInFloor[pathInFloor.Count - 1];
                    desPos[2] += 0.5f;
                    desObject.transform.localPosition = desPos;
                }
            }
            // 最终终点
            if (res < 0 && navigationIndex == navigationPathes.Count - 1)
            {
                Debug.Log("arrive at destination");
                StopNavigation();
            }
            else if (res < 0)
            {
                //本层楼到终点，需要提示上下楼
                navigationIndex++;
                firstUpdate = true;
                autoLabelController.RemoveLabel("navigation");
                notificationManager.GenerateNotification($"前往{navigationFloors[tmpLabelFloor + 1]}楼，请注意安全");
            }
        }
    }

    int GetTypeOfEndInFloor(int nIndex)
    {
        int typeIndex = 0;
        for (int i = 0; i <= nIndex; i++)
        {
            typeIndex += navigationPathes[i].Count;
        }
        return pathType[typeIndex - 1];
    }

    List<Vector3> GetPathInFloor()
    {
        List<Vector3> res = navigationPathes[navigationIndex];
        while (res.Count <= 1)
        {
            navigationIndex++;
            firstUpdate = true;
            autoLabelController.RemoveLabel("navigation");
            if (navigationIndex >= navigationPathes.Count)
            {
                return res;
            }
            res = navigationPathes[navigationIndex];
        }
        return res;
    }

    void Test()
    {
        List<Vector3> tmp = new List<Vector3>();
        tmp.Add(new Vector3(0, 0, -5));
        tmp.Add(new Vector3(1, 0, -5));
        tmp.Add(new Vector3(2, 0, -5));
        //tmp.Add(new Vector3(3.69f, -21.25f, -5.8f));
        // CallNavigation(tmp, "des test");
    }

    public bool GetNavigationState()
    {
        if (m_navigationState == NavigationState.Navigating)
        {
            return true;
        }
        return false;
    }

    public void InitPlaces(Dictionary<int, List<string>> places)
    {
        this.places = places;
        floors = new();
        foreach (var item in places)
        {
            floors.Add(item.Key);
        }
    }

    public void QueryPath(string s)
    {
        if (!places[NavigationDesFloor].Contains(s))
        {
            Debug.Log("Place not valid");
            return;
        }
        Pose cameraPose = poseManager.GetUserPose();
        Pose cameraPose_right = poseManager.Pose_Left2Right(cameraPose);
        float[] tvec = new float[] { cameraPose_right.position[0], cameraPose_right.position[1], cameraPose_right.position[2] };
        float[] qvec = new float[] { cameraPose_right.rotation.w, cameraPose_right.rotation.x, cameraPose_right.rotation.y, cameraPose_right.rotation.z };

        JObject Jpose = new();
        Jpose.Add("tvec", JToken.FromObject(tvec));
        Jpose.Add("qvec", JToken.FromObject(qvec));

        JObject navigationMsg = new();
        navigationMsg.Add("Destination", JToken.FromObject(s));
        navigationMsg.Add("Pose", Jpose);
        navigationMsg.Add("DesFloor", NavigationDesFloor);
        navigationMsg.Add("SrcFloor", poseManager.userCurrentFloor);
        navigationMsg.Add("Mode", navigationMode);

        string json = JsonConvert.SerializeObject(navigationMsg);
        Network network = GameObject.Find("Managers").GetComponent<Network>();
        network.SendText(json, Network.PackageType.navRequest);
        destinationStr = s;
    }

    public void SwitchFloor(int floor)
    {
        m_TargetsList.GetComponent<NavigationTargetListControl>().GenerateButtons(places[floor]);
        NavigationDesFloor = floor;
    }

    public void ToggleTargetsList()
    {
        if (m_TargetsList.activeInHierarchy)
        {
            m_TargetsList.SetActive(false);
            if (m_ShowListIcon != null && m_TargetsListIcon != null)
            {
                m_TargetsListIcon.sprite = m_ShowListIcon;
            }
            toggleText = "显示导航目的地";
        }
        else
        {
            m_TargetsList.SetActive(true);
            // TODO: 增加不同楼层的显示
            // m_TargetsList.GetComponent<NavigationTargetListControl>().GenerateButtons(places[1]);
            TabController tabController = FindObjectOfType<TabController>();

            tabController.InitTab(floors, poseManager.userCurrentFloor);

            if (m_SelectTargetIcon != null && m_TargetsListIcon != null)
            {
                m_TargetsListIcon.sprite = m_SelectTargetIcon;
            }
            toggleText = "请选取目的地";
        }
    }

    public void StopNavigation()
    {
        // m_navigationActive = false;
        autoLabelController.RemoveLabel("navigation");
        m_navigationState = NavigationState.NotNavigating;
        notificationManager.GenerateSuccess("导航停止");
        SendStopNavigation();
        Destroy(desObject);
    }

    public void SendStopNavigation()
    {
        JObject navigationMsg = new();
        navigationMsg.Add("Destination", JToken.FromObject(""));
        string json = JsonConvert.SerializeObject(navigationMsg);

        Network network = FindObjectOfType<Network>();
        network.SendText(json, Network.PackageType.navRequest);
    }

    public void CallNavigation(List<Vector3> path, List<int> floor, List<int> type, string des)
    {
        lock (this)
        {
            destination = des;
            navigationStartPath = path;
            pathFloor = floor;
            pathType = type;
            navigationStartFlag = true;
        }
    }

    public void StartNavigation(List<Vector3> path_right)
    {
        this.path.Clear();
        Debug.Log($"StartNavigation: {path_right.Count}");
        // m_navigationActive = true;
        for (int i = 0; i < path_right.Count; i++)
        {
            // this.path.Add(poseManager.Pose_Right2Left(new Pose(path[i], new Quaternion(0, 0, 0, 1))).position); //成为了unity世界坐标
            this.path.Add(poseManager.Pose_Right2Left(new Pose(path_right[i], new Quaternion(0, 0, 0, 1))).position); //成为了unity世界坐标
        }
        // FindObjectOfType<StateController>().NavigationStatus_Navigating();

        m_navigationState = NavigationState.Navigating;
        notificationManager.GenerateSuccess("导航开始");
        navigationPathes = new();
        navigationFloors = new();
        List<Vector3> tmpPath = new();
        int nowFloor = pathFloor[0];
        navigationFloors.Add(nowFloor);
        for (int i = 0; i < this.path.Count; i++)
        {
            if (pathFloor[i] == nowFloor)
            {
                tmpPath.Add(this.path[i]);
            }
            else
            {
                navigationPathes.Add(tmpPath);
                navigationFloors.Add(pathFloor[i]);
                tmpPath = new();
                tmpPath.Add(this.path[i]);
                nowFloor = pathFloor[i];
            }
        }
        navigationPathes.Add(tmpPath);
        navigationFloors.Add(pathFloor[this.path.Count - 1]);
        navigationIndex = 0;
        firstUpdate = true;
    }

    private void UpdateNavigationUI(NavigationState navigationState)
    {
        switch (navigationState)
        {
            case NavigationState.NotNavigating:
                m_StopNavigationButton.SetActive(false);
                pathRoot.SetActive(false);
                if (m_TargetsListText != null)
                {
                    m_TargetsListText.text = toggleText;
                    m_TargetsListText.color = Color.white;
                }
                break;
            case NavigationState.Navigating:
                m_StopNavigationButton.SetActive(true);
                pathRoot.SetActive(true);
                if (m_TargetsListText != null)
                {
                    m_TargetsListText.text = "To " + destination;
                    m_TargetsListText.color = Color.green;
                }
                break;
        }
    }
    public void ChangeNavigationMode()
    {
        GameObject navSetting = GameObject.Find("navMode");
        TMP_Dropdown dropdown = navSetting.GetComponentInChildren<TMP_Dropdown>();
        navigationMode = dropdown.value;
        Debug.Log("navigationMode changed: " + navigationMode);
    }
}
