using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NavigationTargetListButton : MonoBehaviour
{
    public GameObject targetObject = null;

    [SerializeField] TextMeshProUGUI m_TextMeshProUGUI = null;
    [SerializeField] Image m_Image = null;

    private string targetName = null;

    public void SetText(string text)
    {
        targetName = text;
        if (m_TextMeshProUGUI != null)
        {
            m_TextMeshProUGUI.text = targetName;
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (m_Image != null)
        {
            m_Image.sprite = icon;
        }
    }

    public void SetTarget(GameObject go)
    {
        targetObject = go;
    }

    public void OnClick()
    {
        Debug.Log("On click");
        NavigationManager navigationManager = FindObjectOfType<NavigationManager>();
        navigationManager.QueryPath(this.m_TextMeshProUGUI.text);
        navigationManager.ToggleTargetsList();
        // base.OnPointerClick(pointerEventData);
    }
}