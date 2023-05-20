using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabController : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject tabButtonPrefab;
    int currentFloor = 0;
    Dictionary<int, GameObject> tabButtons;
    
    public void InitTab(List<int> floors, int defaultFloor = 1){
        ClearTab();
        foreach(int floor in floors){
            GameObject tabButton = Instantiate(tabButtonPrefab, transform);
            tabButton.GetComponent<Button>().onClick.AddListener(delegate{SwitchTab(floor);});
            string tabName = $"{floor}楼";
            if(floor < 0){
                tabName = $"B{0-floor}楼";
            }
            tabButton.GetComponentInChildren<TMP_Text>().text = tabName;
            tabButtons.Add(floor, tabButton);
            // tabButton.GetComponent<TabButton>().Init(floor);
        }
        SwitchTab(defaultFloor);
    }

    void ClearTab(){
        foreach(int key in tabButtons.Keys){
            Destroy(tabButtons[key]);
        }
        tabButtons.Clear();
        currentFloor = 0;
    }

    void SwitchTab(int floor){
        if(currentFloor == floor){
            return;
        }
        foreach(int key in tabButtons.Keys){
            tabButtons[key].GetComponentInChildren<TMP_Text>().color = Color.white;
        }
        tabButtons[floor].GetComponentInChildren<TMP_Text>().color = Color.green;
        Debug.Log($"switch to {floor}");
        currentFloor = floor;
        NavigationManager navigationManager = FindObjectOfType<NavigationManager>();
        navigationManager.SwitchFloor(floor);
    }

    private void Awake() {
        tabButtons = new Dictionary<int, GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
