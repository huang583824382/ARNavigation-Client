using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System;

public class StateController : MonoBehaviour
{
    [SerializeField] ARSession mARSession;

    public enum LocSystemStatus
    {
        Uninitialized,
        Ready,
        NeedRelocation,
        LocationExpired
        // LocationFailed
    }

    public enum NetworkStatus
    {
        Disconnected,
        Connected,
        Sharing
    }

    public enum NavigationStatus
    {
        IDLE,
        Navigating
    }
    [SerializeField]
    TMP_Text locSystemStatusTMPt;
    [SerializeField]
    TMP_Text networkStatusTMPt;
    [SerializeField]
    TMP_Text navigationStatusTMPt;

    [SerializeField]
    Sprite disconnectedIcon;
    [SerializeField]
    Sprite connectedIcon;
    [SerializeField]
    Sprite locReadyIcon;
    [SerializeField]
    Sprite locNeedRelocIcon;
    [SerializeField]
    Sprite locExpiredIcon;

    [SerializeField]
    Image networkStatusImage;
    [SerializeField]
    Image locStatusImage;
    public LocSystemStatus locSystemStatus;
    public NetworkStatus networkStatus;
    public NavigationStatus navigationStatus;
    int maxLostTrackingCount = 15;
    float maxMoveDistance = 20;
    int maxLocInterval = 30;
    int maxNeedRelocTime = 30; //after need reloc 30s change to expired
    int lostTrackingCount = 0;
    PoseManager poseManager;
    NotificationManager notificationManager;
    DateTime locReadyStartTime;
    DateTime locNeedRelocStartTime;
    

    // Start is called before the first frame update
    void Start()
    {
        // networkStatusTMPt = gameObject.transform.Find("NetworkStatus").gameObject.GetComponent<TMP_Text>();
        // locSystemStatusTMPt = gameObject.transform.Find("LocSystemStatus").gameObject.GetComponent<TMP_Text>();
        // navigationStatusTMPt = gameObject.transform.Find("NavigationStatus").gameObject.GetComponent<TMP_Text>();

        poseManager = FindObjectOfType<PoseManager>();
        notificationManager = FindObjectOfType<NotificationManager>();
        NetworkStatus_Disconnected();
        LocSystemStatus_Uninitialized();
        NavigationStatus_IDLE();
        // if(Application.isEditor){
        //     Invoke("TestResetAR", 4f);
        // }
    }

    public void OnARSessionResetClick(){
        Debug.Log("ARSession Reset");
        mARSession.Reset();
    }
    
    // void TestResetAR(){
    //     mARSession.Reset();
    // }

    // Update is called once per frame
    void Update()
    {
        // Check ARSession status
        if(mARSession.subsystem != null){
            if(mARSession.subsystem.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Limited){
                lostTrackingCount++;
            }
            else{
                lostTrackingCount = 0;
            }
        }
        if(lostTrackingCount >= maxLostTrackingCount){
            LocSystemStatus_Expired();
            lostTrackingCount = 0;
        }
        float ratio = 1f;
        if(poseManager.num_inliers < 100){
            ratio = 0.5f;
        }
        switch(locSystemStatus){
            case LocSystemStatus.Ready:
                if(poseManager.GetDistanceFromLastLocPosition()>=maxMoveDistance*ratio){
                    LocSystemStatus_NeedRelocation();
                }
                if((DateTime.Now - locReadyStartTime) >= TimeSpan.FromSeconds(maxLocInterval*ratio)){
                    LocSystemStatus_NeedRelocation();
                }
                break;
            case LocSystemStatus.NeedRelocation:
                if((DateTime.Now - locNeedRelocStartTime) >= TimeSpan.FromSeconds(maxNeedRelocTime*ratio)){
                    LocSystemStatus_Expired();
                }
                break;
            case LocSystemStatus.LocationExpired:
                break;
        }
    }

    // void Test(){
    //     Debug.Log(mARSession.subsystem.trackingState);
    // }

    public void NetworkStatus_Disconnected()
    {
        networkStatus = NetworkStatus.Disconnected;
        if(networkStatusTMPt != null){
            networkStatusTMPt.text = "Disconnected";
            networkStatusTMPt.color = new Color(255, 0, 0);
        }
        networkStatusImage.sprite = disconnectedIcon;
    }

    public void NetworkStatus_Connected()
    {
        networkStatus = NetworkStatus.Connected;
        if(networkStatusTMPt != null){
            networkStatusTMPt.text = "Connected";
            networkStatusTMPt.color = new Color(0, 255, 0);
        }
        networkStatusImage.sprite = connectedIcon;
    }

    public void NetworkStatus_Sharing()
    {
        networkStatus = NetworkStatus.Sharing;
        if(networkStatusTMPt != null){
            networkStatusTMPt.text = "Sharing";
            networkStatusTMPt.color = new Color(0, 0, 255);
        }
    }

    public void LocSystemStatus_Uninitialized()
    {
        notificationManager.CallNotification(CreateNotification.NotificationType.Warning, "定位初始化中...\n请用手机摄像头平稳捕捉周围场景", 10f);
        locSystemStatus = LocSystemStatus.Uninitialized;
        if(locSystemStatusTMPt != null){
            locSystemStatusTMPt.color = new Color(255, 0, 0);
            locSystemStatusTMPt.text = "Uninitialized";
        }
        locStatusImage.sprite = locExpiredIcon;
        poseManager.AddCamPoseState = PoseManager.AddCamPoseStateEnum.WaitingFirst;
        poseManager.AddCamPoseState = PoseManager.AddCamPoseStateEnum.Normal;
    }

    public void LocSystemStatus_Ready()
    {
        Debug.Log("LocSystemStatus_Ready");
        locSystemStatus = LocSystemStatus.Ready;
        if(locSystemStatusTMPt != null){
            locSystemStatusTMPt.color = new Color(0, 255, 0);
            locSystemStatusTMPt.text = "Ready";
        }
        locStatusImage.sprite = locReadyIcon;
        locReadyStartTime = DateTime.Now;
    }

    public void LocSystemStatus_NeedRelocation()
    {
        // 只能从Ready来
        notificationManager.CallNotification(CreateNotification.NotificationType.Warning, "请更新定位以获取更好服务体验", 2f);

        Debug.Log("LocSystemStatus_NeedRelocation");
        if(locSystemStatus != LocSystemStatus.Ready){
            Debug.Log("Not from Ready stats, skip");
            return;
        }
        locSystemStatus = LocSystemStatus.NeedRelocation;
        if(locSystemStatusTMPt != null){
            locSystemStatusTMPt.color = new Color(255, 255, 0);
            locSystemStatusTMPt.text = "Need Relocation";
        }
        locStatusImage.sprite = locNeedRelocIcon;
        locNeedRelocStartTime = DateTime.Now;
    }

    public void LocSystemStatus_Expired()
    {
        notificationManager.CallNotification(CreateNotification.NotificationType.Error, "重定位中...\n请用手机摄像头平稳捕捉周围场景重新定位", 10f);
        Debug.Log("LocSystemStatus_Expired");
        locSystemStatus = LocSystemStatus.LocationExpired;
        if(locSystemStatusTMPt != null){
            locSystemStatusTMPt.color = new Color(255, 0, 0);
            locSystemStatusTMPt.text = "Location Expired";
        }
        locStatusImage.sprite = locExpiredIcon;
        // poseManager.AddCamPoseState = PoseManager.AddCamPoseStateEnum.WaitingFirst;
        poseManager.AddCamPoseState = PoseManager.AddCamPoseStateEnum.Normal;
    }

    // public void LocSystemStatus_LocationFailed()
    // {
    //     locSystemStatus = LocSystemStatus.LocationFailed;
    //     if(locSystemStatusTMPt != null){
    //         locSystemStatusTMPt.color = new Color(255, 0, 0);
    //         locSystemStatusTMPt.text = "Locate Failed";
    //     }
    //     locStatusImage.sprite = locExpiredIcon;
    // }

    public void NavigationStatus_IDLE()
    {
        navigationStatus = NavigationStatus.IDLE;
        if(navigationStatusTMPt != null){
            navigationStatusTMPt.text = "IDLE";
            navigationStatusTMPt.color = new Color(0, 0, 255);
        }
    }

    public void NavigationStatus_Navigating()
    {
        navigationStatus = NavigationStatus.Navigating;
        if(navigationStatusTMPt != null){
            navigationStatusTMPt.text = "Navigating";
            navigationStatusTMPt.color = new Color(0, 255, 0);
        }
    }


}
