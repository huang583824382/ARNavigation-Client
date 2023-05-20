using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AdManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] GameObject AdsRoot;
    [SerializeField] GameObject AdPrefab;
    Network network;
    PoseManager poseManager;
    List<int> ad_ids;
    List<string> ad_names;
    List<string> ad_urls;
    List<Vector3> ad_positions;
    List<int> ad_floors;
    List<byte[]> ad_images;
    bool isShowAdsCalled = false;
    Dictionary<int, GameObject> ads = new Dictionary<int, GameObject>();
    [SerializeField] ARRaycastManager m_RaycastManager;
    List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();
    bool setting_AdShow = true;

    void Start()
    {
        network = GetComponent<Network>();
        poseManager = GetComponent<PoseManager>();
        ad_ids = new List<int>();
        ad_names = new List<string>();
        ad_urls = new List<string>();
        ad_positions = new List<Vector3>();
        ad_floors = new List<int>();
        ad_images = new List<byte[]>();
    }

    // Update is called once per frame
    void Update()
    {
        if(isShowAdsCalled){
            ShowAds();
            isShowAdsCalled = false;
        }
    }

    public void AddAdsToShow(int id, string name, string url, Vector3 position, byte[] image, int floor){
        ad_ids.Add(id);
        ad_names.Add(name);
        ad_urls.Add(url);
        ad_positions.Add(poseManager.Pose_Right2Left(new Pose(position, new Quaternion(0, 0, 0, 1))).position);
        ad_images.Add(image);
        ad_floors.Add(floor);
    }

    public void CallAdsShow(){
        isShowAdsCalled = true;
    }

// 传入image数据
    void ShowAds(){
        Debug.Log("ShowAds");
        ClearAds();
        for(int i = 0; i < ad_ids.Count; i++){
            GameObject ad = Instantiate(AdPrefab, AdsRoot.transform);
            ad.transform.localPosition = ad_positions[i];
            ad.GetComponent<AdController>().SetAd(ad_names[i], ad_urls[i], ad_images[i]);
            Debug.Log($"add ads {ad_ids[i]}");
            ads.Add(ad_ids[i], ad);
        }
        ad_ids.Clear();
        ad_names.Clear();
        ad_urls.Clear();
        ad_positions.Clear();
        ad_images.Clear();
        ad_floors.Clear();
    }

    void ClearAds(){
        foreach(int key in ads.Keys){
            Destroy(ads[key]);
        }
        ads.Clear();
    }

    public void ToggleAdsShow(){
        setting_AdShow = !setting_AdShow;
        AdsRoot.SetActive(setting_AdShow);
    }

}
