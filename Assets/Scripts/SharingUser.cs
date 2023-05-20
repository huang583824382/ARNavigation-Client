using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharingUser : MonoBehaviour
{
    GameObject userPrefab;
    Pose pose;
    GameObject userLabel;
    PoseManager poseManager;
    int floor = 0;
    public ShareManager.LocStatus locStatue;
    ShareUserPrefabController shareUserPrefabController;

    public void Init(string name, Pose pose, GameObject userPrefab){
        poseManager = GameObject.Find("Managers").GetComponent<PoseManager>();
        this.pose = poseManager.Pose_Map2Global(poseManager.Pose_Right2Left(pose));
        Debug.Log("SharingUser pose: " + this.pose.position+" "+pose.position);
        this.userPrefab = userPrefab;
        userLabel = GameObject.Instantiate(userPrefab, gameObject.transform);
        userLabel.name = "User";
        shareUserPrefabController = userLabel.GetComponent<ShareUserPrefabController>();
        shareUserPrefabController.Init(name, 0, this.pose.position);
    }

    public void UpdatePose(Pose leftPose, int floor){
        this.pose = leftPose;
        this.floor = floor;
        shareUserPrefabController.UpdatePosition(this.pose.position);
    }

    public void UpdateStatus(ShareManager.LocStatus status){
        locStatue = status;
        int stateIndex = 0;
        switch (locStatue)
        {
            case ShareManager.LocStatus.Good:
                stateIndex = 0;
                break;
            case ShareManager.LocStatus.Bad:
                stateIndex = 1;
                break;
            default:
                stateIndex = 2;
                break;
        }
        shareUserPrefabController.ChangeState(stateIndex);
    }
}
