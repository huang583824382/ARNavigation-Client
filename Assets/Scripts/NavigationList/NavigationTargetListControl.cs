using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(ScrollRect))]
public class NavigationTargetListControl : MonoBehaviour
{
    [SerializeField]
    private GameObject m_ButtonTemplate = null;
    [SerializeField]
    private RectTransform m_ContentParent = null;
    [SerializeField]
    int m_MaxButtonsOnScreen = 4;
    private List<GameObject> m_Buttons = new List<GameObject>();

    public void GenerateButtons(List<string> targetNames)
    {
        if (m_Buttons.Count > 0)
        {
            DestroyButtons();
        }
        if(targetNames.Count == 0)
        {
            return;
        }

        foreach (string targetName in targetNames)
        {
            GameObject button = Instantiate(m_ButtonTemplate, m_ContentParent);
            m_Buttons.Add(button);
            button.SetActive(true);
            button.name = "button " + targetName;

            NavigationTargetListButton navigationTargetListButton = button.GetComponent<NavigationTargetListButton>();
            navigationTargetListButton.SetText(targetName);
        }


        // calculate lists RectTransform size
        float x = m_ButtonTemplate.GetComponent<RectTransform>().sizeDelta.x;
        float y = m_ButtonTemplate.GetComponent<RectTransform>().sizeDelta.y * Mathf.Min(m_Buttons.Count, m_MaxButtonsOnScreen);
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(x, y);

        ScrollToTop();
    }

    private void DestroyButtons()
    {
        foreach (GameObject button in m_Buttons)
        {
            Destroy(button);
        }
        m_Buttons.Clear();
    }

    private void ScrollToTop()
    {
        transform.GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
    }
}
