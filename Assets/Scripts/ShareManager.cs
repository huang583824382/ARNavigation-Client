using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using TMPro;
public class ShareManager : MonoBehaviour
{
    [SerializeField] GameObject userPrefab;
    [SerializeField] GameObject usersRoot;
    [SerializeField] Sprite iconActive;
    [SerializeField] Sprite iconInactive;
    [SerializeField] Image shareButtonIcon;
    [SerializeField] GameObject ShareUserListPanel;
    [SerializeField] GameObject ShareUserListItem;
    [SerializeField] GameObject ShareUserList;
    public enum LocStatus
    {
        Uninitialized,
        Good,
        Bad,
        Unreliable,
    }
    Dictionary<string, SharingUser> shareUsersDict = new Dictionary<string, SharingUser>();
    Dictionary<string, Pose> shareUsersPose = new Dictionary<string, Pose>();
    Dictionary<string, GameObject> shareUserListItems = new Dictionary<string, GameObject>();
    PoseManager poseManager;
    Network network;
    MiniMapManager miniMapManager;
    bool callParsing = false;
    string parsingStr;
    public bool sharing = false;
    string userName;
    // Start is called before the first frame update
    void Start()
    {
        poseManager = gameObject.GetComponent<PoseManager>();
        network = gameObject.GetComponent<Network>();
        userName = network.userName;
        miniMapManager = FindObjectOfType<MiniMapManager>();
        if(Application.isEditor)
            Invoke("Test", 5f);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateShareUI();
        lock (this)
        {
            if (callParsing)
            {
                callParsing = false;
                ParseBroadcast(parsingStr);
            }
        }
    }

    void UpdateShareUI(){
        if(sharing){
            shareButtonIcon.sprite = iconActive;
        }else{
            shareButtonIcon.sprite = iconInactive;
        }
    }

    public void Test(){
        string test = "{'Sharing': [{'name': 'Mike', 'pose': {'tvec': [-8.6190845, 14.6241837, -17.01165161], 'qvec': [-0.243785, 0.850593865, -0.445609719, 0.135981649]}, 'floor': -1, 'state': {'status': 1, 'locStatus': 3}}]}";
        ParseBroadcast(test);
    }

    void AddShareUser(string name, Pose pose){
        if(shareUsersDict.ContainsKey(name)){
            return;
        }
        var userRoot = new GameObject(name);
        userRoot.transform.SetParent(usersRoot.transform);
        userRoot.AddComponent<SharingUser>();
        userRoot.transform.localRotation = Quaternion.identity;
        userRoot.transform.localPosition = Vector3.zero;
        SharingUser sharingUser = userRoot.GetComponent<SharingUser>();
        sharingUser.Init(name, pose, userPrefab);
        
        shareUsersDict.Add(name, sharingUser);
        shareUsersPose.Add(name, pose);

        GameObject listItem = Instantiate(ShareUserListItem, ShareUserList.transform);
        listItem.transform.Find("Name").gameObject.GetComponent<TMP_Text>().text = name;
        listItem.transform.Find("Floor").gameObject.GetComponent<TMP_Text>().text = "F0";
        listItem.transform.Find("State").gameObject.GetComponent<TMP_Text>().text = "Ready";
        shareUserListItems.Add(name, listItem);
        miniMapManager.UpdatePointerOfShareUser(name, pose, 0);
    }

    void RemoveShareUser(string name){
        if(shareUsersDict.ContainsKey(name)){
            Destroy(shareUsersDict[name].gameObject);
            shareUsersDict.Remove(name);
            shareUsersPose.Remove(name);
            Destroy(shareUserListItems[name]);
            shareUserListItems.Remove(name);
        }
    }

    void UpdateShareUserPose(string name, Pose pose, int floor){
        if(shareUsersDict.ContainsKey(name)){
            Pose leftp = poseManager.Pose_Right2Left(pose);
            shareUsersPose[name] = leftp;
            shareUsersDict[name].UpdatePose(leftp, floor);
            shareUserListItems[name].transform.Find("Floor").gameObject.GetComponent<TMP_Text>().text = floor.ToString()+"L";
            miniMapManager.UpdatePointerOfShareUser(name, leftp, floor);
        }
    }

    void UpdateShareUserStatus(string name, LocStatus status){
        if(shareUsersDict.ContainsKey(name)){
            shareUsersDict[name].UpdateStatus(status);
            shareUserListItems[name].transform.Find("State").gameObject.GetComponent<TMP_Text>().text = status.ToString();
        }
    }

    public void CallParseBroadcast(string json_str){
        lock (this)
        {
            parsingStr = json_str;
            callParsing = true;
        }
    }

    public void ParseBroadcast(string json_str){
        JObject json = JObject.Parse(json_str);
        Dictionary<string, SharingUser> tmpDict = new Dictionary<string, SharingUser>();
        foreach(var item in json["Sharing"]){
            string name = item["name"].ToString();
            if(name == userName) continue;
            if(shareUsersDict.ContainsKey(name)){
                tmpDict.Add(name, shareUsersDict[name]);
                shareUsersDict.Remove(name);
            }
            else{
                AddShareUser(name, new Pose());
                tmpDict.Add(name, shareUsersDict[name]);
                shareUsersDict.Remove(name);
            }
        }
        //shareUsersDict中的用户不在json中，说明已经离开了共享
        foreach(var item in shareUsersDict){
            RemoveShareUser(item.Key);
        }
        shareUsersDict = tmpDict;
        foreach(var item in json["Sharing"]){
            string name = item["name"].ToString();
            if(name == userName) continue;
            float[] tvec = item["pose"]["tvec"].ToObject<float[]>();
            float[] qvec = item["pose"]["qvec"].ToObject<float[]>();
            int floor = item["floor"].ToObject<int>();
            Pose pose = new Pose(new Vector3(tvec[0], tvec[1], tvec[2]), new Quaternion(qvec[1], qvec[2], qvec[3], qvec[0]));
            UpdateShareUserPose(item["name"].ToString(), pose, floor);
            int locStatusNum = item["state"]["locStatus"].ToObject<int>();
            LocStatus locStatus = LocStatus.Uninitialized;
            switch (locStatusNum)
            {
                case 0: locStatus = LocStatus.Uninitialized; break;
                case 1: locStatus = LocStatus.Good; break;
                case 2: locStatus = LocStatus.Bad; break;
                case 3: locStatus = LocStatus.Unreliable; break;
            }
            UpdateShareUserStatus(item["name"].ToString(), locStatus);
        }
    }
    public void StartShare(){
        sharing = true;
        JObject shareMsg = new();
        shareMsg.Add("Share", JToken.FromObject(true));
        string json = JsonConvert.SerializeObject(shareMsg);
        Network network = gameObject.GetComponent<Network>();
        network.SendText(json, Network.PackageType.sharePos);
        userName = network.userName;
        ShareUserListPanel.SetActive(true);
        usersRoot.SetActive(true);
    }

    public void StopShare(){
        sharing = false;
        JObject shareMsg = new();
        shareMsg.Add("Share", JToken.FromObject(false));
        string json = JsonConvert.SerializeObject(shareMsg);
        Network network = gameObject.GetComponent<Network>();
        network.SendText(json, Network.PackageType.sharePos);
        ShareUserListPanel.SetActive(false);
        usersRoot.SetActive(false);
        miniMapManager.ClearShareUserPointers();
    }

    public void OnShareClick(){
        if(sharing){
            StopShare();
        }
        else{
            StartShare();
        }
    }
}
