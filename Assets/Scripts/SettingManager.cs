using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [SerializeField] Button settingButton;
    [SerializeField] GameObject settingPanel;
    // Start is called before the first frame update
    void Start()
    {
        settingPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSettingButtonToggle(){
        bool isActive = settingPanel.activeSelf;
        settingPanel.SetActive(!isActive);
    }
}
