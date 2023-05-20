using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    string userName;
    [SerializeField] TMP_InputField inputField;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnLoginClick(){
        userName = inputField.text;
        if(userName != ""){
            PlayerPrefs.SetString("userName", userName);
            PlayerPrefs.Save();
            SceneManager.LoadScene("AR Navigation");
        }
        else{

        }
    }
}
