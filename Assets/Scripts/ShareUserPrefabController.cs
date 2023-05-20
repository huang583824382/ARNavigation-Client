using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class ShareUserPrefabController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image image1;
    [SerializeField] Image image2;
    Vector3 position;
    List<Color> colors;
    Camera mainCamera;
    float distanceValue;
    float scaleInitial = 0.0005f;
    float maxDistance = 10f;
    PoseManager poseManager;
    void Start()
    {
        mainCamera = Camera.main;
        scaleInitial = transform.localScale.x;
        poseManager = GameObject.Find("Managers").GetComponent<PoseManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void LateUpdate()
    {
        // Debug.Log(mainCamera.transform.position);
        Quaternion q = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        transform.rotation = q;
        Vector3 userPos = poseManager.GetUserPose().position;
        distanceValue = Vector3.Distance(position, userPos);

        if(distanceValue > maxDistance){
            transform.localPosition = userPos + (position - userPos).normalized * maxDistance;
        }
        else{
            transform.localPosition = position;
        }
    }
    
    public void Init(string name, int stateIndex, Vector3 p){
        position = p;
        colors = new();
        colors.Add(new Color(90/255f, 191/255f, 255/255f, 224/255f)); // blue
        colors.Add(new Color(255/255f, 255/255f, 90/255f, 224/255f)); // yellow
        colors.Add(new Color(255/255f, 90/255f, 90/255f, 224/255f)); // red
        nameText.text = name;
        image1.color = colors[stateIndex];
        image2.color = colors[stateIndex];
    }

    public void UpdatePosition(Vector3 p){
        position = p;
    }

    public void ChangeState(int stateIndex){
        image1.color = colors[stateIndex];
        image2.color = colors[stateIndex];
    }

}
