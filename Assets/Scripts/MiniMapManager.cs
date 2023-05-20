using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniMapManager : MonoBehaviour
{
    [SerializeField] GameObject pointer;
    [SerializeField] GameObject map;
    [SerializeField] GameObject dropDown_obj;
    [SerializeField] List<Sprite> floorSprites;
    Dictionary<string, GameObject> shareUsers;
    Dictionary<string, GameObject> shareUserLabelNames;
    Dictionary<string, int> shareUserFloors;
    Vector3 mapOrigin = new Vector3(2.11f, -5.684f, 0);
    float ratio = 14.25f;
    int windowWidth;
    int windowHeight;
    Vector3 initPosition;
    PoseManager poseManager;
    bool bigMode = false;
    bool touchStart = false;
    bool Dragged = false;
    Vector3 lastMousePosition;
    Vector3 lastTouchPosition;
    Vector3 mapOriginPosition;
    List<int> floors;
    int miniMapFloor = 0;

    [SerializeField] GameObject dragMap;
    // Start is called before the first frame update
    void Start()
    {
        shareUsers = new();
        shareUserLabelNames = new();
        shareUserFloors = new();
        poseManager = FindObjectOfType<PoseManager>();
        floors = new List<int>();
        floors.Add(-1);
        floors.Add(1);
        floors.Add(2);
        floors.Add(3);
        floors.Add(4);

        //获取当前组件的高度和宽度
        // RectTransform rect = gameObject.GetComponent<RectTransform>();
        // windowWidth = (int)rect.rect.width/2;
        // windowHeight = (int)rect.rect.height/2;
        // initPosition = map.transform.localPosition;
        // Debug.Log(initPosition);
        InitDropdown(floors);
        SetMapSprite(floorSprites[0]);
        // Test();
    }

    // void Test(){
    //     List<Vector3> path = new List<Vector3>();
    // //     [
    // //     [
    // //         -7.184144936876114,
    // //         2.945478369433017,
    // //         -17.4
    // //     ],
    // //     [
    // //         -7.363702489765286,
    // //         -5.434842690582761,
    // //         -17.4
    // //     ],
    // //     [
    // //         -7.984539368156013,
    // //         -34.410614168320976,
    // //         -17.4
    // //     ],
    // //     [
    // //         -6.547884428322669,
    // //         -34.44600323292428,
    // //         -17.4
    // //     ],
    // //     [
    // //         -6.6599498010831715,
    // //         -38.9954244785978,
    // //         -17.4
    // //     ]
    // // ]
    //     path.Add(new Vector3(-7.184144936876114f, -2.945478369433017f, -17.4f));
    //     path.Add(new Vector3(-7.363702489765286f, 5.434842690582761f, -17.4f));
    //     path.Add(new Vector3(-7.984539368156013f, 34.410614168320976f, -17.4f));
    //     path.Add(new Vector3(-6.547884428322669f, 34.44600323292428f, -17.4f));
    //     path.Add(new Vector3(-6.6599498010831715f, 38.9954244785978f, -17.4f));
    //     GenerateMiniMapPath(path);
    // }

    // Update is called once per frame
    void Update()
    {
        UpdatePointerDirection(poseManager.GetUserPose());
        UpdatePointerPosition(poseManager.GetUserPose().position);
        UpdateShareUserVisable();
        // UpdatePointerPosition(new Vector3(-6.978978988885665f, 21.122111399601707f, -14.7f));
        if(!bigMode) CenterPointer();
        // Debug.Log(dragMap.transform.position);
        if (Input.touchCount > 0 && bigMode == true)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStart = true;
                    Dragged = false;
                    // Debug.Log("begin");
                    // 停止正在延时的代码
                    // mapOriginPosition = dragMap.transform.localPosition;
                    break;

                case TouchPhase.Moved:
                    if (touchStart)
                    {
                        CancelInvoke("ResetMapPosition");
                        Dragged = true;
                        // Debug.Log("Dragging");
                        Vector3 deltaPosition = touch.deltaPosition;
                        dragMap.transform.position += new Vector3(deltaPosition.x, deltaPosition.y, 0);
                    }
                    break;

                case TouchPhase.Ended:
                    if(touchStart){
                        if(Dragged){
                            // 延时5s后执行下面的代码
                            Invoke("ResetMapPosition", 3f);
                        }
                    }
                    touchStart = false;
                    break;
            }
        }
    }

    // public void Test(){
    //     UpdatePointerDirection(poseManager.GetUserPose().rotation);
    // }

    void ResetMapPosition(){
        // if(bigMode == true){
        //     dragMap.transform.localPosition = new Vector3(-400, -400, 0);
        // }
        // else{
        //     dragMap.transform.localPosition = new Vector3(-200, -200, 0);
        // }
        // // dragMap.transform.localPosition = new Vector3(0, 0, 0);
        CenterPointer();
    }

    // public void UpdatePointerDirection(Pose p){
    //     // 问题：更新地图后并不会调整角度
    //     Matrix4x4 rotationMatrix = Matrix4x4.Rotate(p.rotation); //在局部坐标系下的旋转矩阵
    //     Vector3 forwardVector = poseManager.mapRoot.transform.up; //在局部坐标系下的forward向量
    //     Vector3 directionVector = rotationMatrix * forwardVector; //在局部坐标系下的向前的向量
    //     Vector3 directionVector_plane = new Vector3(directionVector.x, directionVector.y, 0).normalized; //在局部坐标系下的向前的向量
    //     // Debug.Log(directionVector_plane);
    //     Vector3 originDirection = new Vector3(1, 0, 0);
    //     Vector3 axis = Vector3.forward;
    //     // Quaternion q = Quaternion.Inverse(Quaternion.FromToRotation(originDirection, directionVector_plane));
    //     Quaternion q = Quaternion.FromToRotation(originDirection, directionVector_plane);
    //     Debug.Log(q.eulerAngles);
    //     Quaternion qres = Quaternion.AngleAxis(q.eulerAngles.z, Vector3.forward);
    //     pointer.transform.rotation = qres;
    // }

    public void UpdatePointerDirection(Pose p){
        Vector3 tmp = new Vector3(p.forward[0], -p.forward[1], 0).normalized;
        Vector3 origind = new Vector3(1, 0, 0);
        Quaternion q = Quaternion.FromToRotation(origind, tmp);
        // Debug.Log(q.eulerAngles.z);
        Quaternion qres = Quaternion.AngleAxis(q.eulerAngles.z-180, Vector3.forward);
        pointer.transform.localRotation = qres;
    }

    public void UpdatePointerPosition(Vector3 pos){
        Vector3 tmp = pos + mapOrigin;
        tmp = tmp*ratio;
        Vector3 pointPos = new Vector3(-tmp.y, -tmp.x, 0);
        pointer.transform.localPosition = pointPos;
        // Debug.Log($"update pointer position input{pos} output {pointPos}");
    }

    Quaternion GetMiniMapRotation(Pose p){
        Vector3 tmp = new Vector3(p.forward[0], -p.forward[1], 0).normalized;
        Vector3 origind = new Vector3(1, 0, 0);
        Quaternion q = Quaternion.FromToRotation(origind, tmp);
        // Debug.Log(q.eulerAngles.z);
        Quaternion qres = Quaternion.AngleAxis(q.eulerAngles.z-180, Vector3.forward);
        return qres;
    }

    Pose GetMiniMapPose(Pose p){
        Vector3 pos = p.position;
        Vector3 tmp = pos + mapOrigin;
        tmp = tmp*ratio;
        Vector3 pointPos = new Vector3(-tmp.y, -tmp.x, 0);

        tmp = new Vector3(p.forward[0], -p.forward[1], 0).normalized;
        Vector3 origind = new Vector3(1, 0, 0);
        Quaternion q = Quaternion.FromToRotation(origind, tmp);
        // Debug.Log(q.eulerAngles.z);
        Quaternion qres = Quaternion.AngleAxis(q.eulerAngles.z-180, Vector3.forward);
        
        // Debug.Log($"get mini map pose input {p} output {pointPos}");
        return new Pose(pointPos, qres);
    }

    public void UpdatePointerOfShareUser(string name, Pose pose, int floor){
        Pose mapPose = GetMiniMapPose(pose);
        // Debug.Log($"update pointer of {name} to {mapPose.position}");
        if(shareUsers.ContainsKey(name)){
            shareUsers[name].transform.localPosition = mapPose.position;
            shareUsers[name].transform.localRotation = mapPose.rotation;
            shareUserLabelNames[name].transform.localPosition = mapPose.position-Vector3.up*50;
            shareUserFloors[name] = floor;
        }
        else{
            // create new user
            GameObject user = Instantiate(pointer, dragMap.transform);
            shareUsers.Add(name, user);
            GameObject label = new GameObject("Label");
            label.AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(dragMap.transform);
            TMP_Text t = label.GetComponent<TMP_Text>();
            t.text = name;
            t.color = new Color(85/255f, 107/255f, 47/255f);
            t.alignment = TextAlignmentOptions.Center;
            shareUserLabelNames.Add(name, label);
            user.transform.localPosition = mapPose.position;
            user.transform.localRotation = mapPose.rotation;
            label.transform.localPosition = mapPose.position-Vector3.up*50;
            shareUserFloors.Add(name, floor);
        }
    }

    void UpdateShareUserVisable(){
        foreach(string name in shareUsers.Keys){
            if(shareUserFloors[name] == miniMapFloor){
                shareUsers[name].SetActive(true);
                shareUserLabelNames[name].SetActive(true);
            }
            else{
                shareUsers[name].SetActive(false);
                shareUserLabelNames[name].SetActive(false);
            }
        }
    }

    void RemovePointerOfShareUser(string name){
        if(shareUsers.ContainsKey(name)){
            Destroy(shareUsers[name]);
            shareUsers.Remove(name);
            Destroy(shareUserLabelNames[name]);
            shareUserLabelNames.Remove(name);
            shareUserFloors.Remove(name);
        }
    }

    public void ClearShareUserPointers(){
        foreach(string name in shareUsers.Keys){
            Destroy(shareUsers[name]);
            Destroy(shareUserLabelNames[name]);
        }
        shareUsers.Clear();
        shareUserLabelNames.Clear();
        shareUserFloors.Clear();
    }

    // public void GenerateMiniMapPath(List<Vector3> path){
    //     List<Vector3> pathPos = new List<Vector3>();
    //     Sprite sprite = map.GetComponent<Image>().sprite;
    //     Texture2D texture = sprite.texture;
    //     for(int i=0; i<path.Count; i++){
    //         Vector3 pointPos = GetMiniMapPosition(path[i]);
    //         pathPos.Add(pointPos);
    //         texture.SetPixel((int)pointPos.x, (int)pointPos.y, Color.red);
    //     }
    //     map.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
        
        
    //     // 

    // }

    public void CenterPointer(){
        Vector3 pointPos = pointer.transform.localPosition;
        Vector3 newPos;
        if(bigMode == true){
            newPos = new Vector3(-pointPos.x-400, -pointPos.y-400, 0);
        }
        else{
            newPos = new Vector3(-pointPos.x-200, -pointPos.y-200, 0);
        }
        dragMap.transform.localPosition = newPos;
    }

    public void OnClick(){
        bigMode = !bigMode;
        Debug.Log($"click miniMap change to big {bigMode}");

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if(bigMode){
            rect.sizeDelta = new Vector2(800, 800);
            dropDown_obj.SetActive(true);
            TMP_Dropdown dropdown = dropDown_obj.GetComponent<TMP_Dropdown>();
            dropdown.value = floors.IndexOf(poseManager.userCurrentFloor);
            
            // GameObject.Find("bigButton").SetActive(false);
        }else{
            dropDown_obj.SetActive(false);
            rect.sizeDelta = new Vector2(400, 400);
            // GameObject.Find("bigButton").SetActive(true);
        }
    }

    void InitDropdown(List<int> floors){
        TMP_Dropdown dropdown = dropDown_obj.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        foreach(int floor in floors){
            dropdown.options.Add(new TMP_Dropdown.OptionData(floor.ToString()));
        }
        dropdown.onValueChanged.AddListener(delegate {
            OnDropdownValueChanged(dropdown);
        });
        dropdown.value = floors.IndexOf(poseManager.userCurrentFloor);
    }

    void OnDropdownValueChanged(TMP_Dropdown dropdown){
        Debug.Log("select floor: " + dropdown.value);
        // change the floor image
        SetMapSprite(floorSprites[dropdown.value]);
        // 获取dropdown的值
        miniMapFloor = floors[dropdown.value];
        if(miniMapFloor != poseManager.userCurrentFloor){
            pointer.SetActive(false);
        }
        else{
            pointer.SetActive(true);
        }
    }

    public void SetMiniMapFloor(int floor){
        SetMapSprite(floorSprites[floors.IndexOf(floor)]);
        TMP_Dropdown dropdown = dropDown_obj.GetComponent<TMP_Dropdown>();
        dropdown.value = floors.IndexOf(floor);
    }

    void SetMapSprite(Sprite sprite){
        map.GetComponent<Image>().sprite = sprite;
    }

}
