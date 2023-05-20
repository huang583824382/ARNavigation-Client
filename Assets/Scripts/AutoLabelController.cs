using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoLabelController : MonoBehaviour
{
    [SerializeField] GameObject labelPrefab;
    Dictionary<string, GameObject> labels = new Dictionary<string, GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateLabel(string name, Vector3 position, string text){
        
        GameObject label = Instantiate(labelPrefab, transform);
        label.transform.localPosition = position;
        label.GetComponentInChildren<TMP_Text>().text = text;
        labels.Add(name, label);
    }

    public void UpdateLabelPos(string name, Vector3 position){
        if(labels.ContainsKey(name)){
            labels[name].transform.localPosition = position;
        }
    }

    public void UpdateLabelText(string name, string text){
        if(labels.ContainsKey(name)){
            labels[name].GetComponentInChildren<TMP_Text>().text = text;
        }
    }

    public void RemoveLabel(string name){
        if(labels.ContainsKey(name)){
            Destroy(labels[name]);
            labels.Remove(name);
        }
    }
}
