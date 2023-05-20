using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    [SerializeField]
    private GameObject m_Notification = null;
    private CreateNotification m_CreateNotification = null;

    public bool haveSomethingToShow = false;
    private CreateNotification.NotificationType somethingType;
    private string somethingText;
    float duration;

    public static NotificationManager Instance
    {
        get
        {
#if UNITY_EDITOR
            if (instance == null && !Application.isPlaying)
            {
                instance = UnityEngine.Object.FindObjectOfType<NotificationManager>();
            }
#endif
            if (instance == null)
            {
                Debug.LogError("No NotificationManager instance found. Ensure one exists in the scene.");
            }
            return instance;
        }
    }

    private static NotificationManager instance = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            m_Notification.SetActive(false);
            m_CreateNotification = m_Notification.GetComponent<CreateNotification>();
        }
        if (instance != this)
        {
            Debug.LogError("There must be only one NotificationManager object in a scene.");
            UnityEngine.Object.DestroyImmediate(this);
            return;
        }
    }

    public void CallNotification(CreateNotification.NotificationType type, string text, float duration = 2f)
    {
        somethingText = text;
        somethingType = type;
        haveSomethingToShow = true;
        this.duration = duration;
    }

    private void Update() {
        lock(this){
            if(haveSomethingToShow){
                haveSomethingToShow = false;
                InitNotification(somethingType, somethingText);
            }
        }
    }

    private void InitNotification(CreateNotification.NotificationType type, string text)
    {
        m_Notification.SetActive(true);
        m_CreateNotification.SetIconAndText(type, text, duration);
    }

    public void GenerateNotification(string text)
    {
        InitNotification(CreateNotification.NotificationType.Info, text);
    }

    public void GenerateWarning(string text)
    {
        InitNotification(CreateNotification.NotificationType.Warning, text);
    }

    public void GenerateError(string text)
    {
        InitNotification(CreateNotification.NotificationType.Error, text);
    }

    public void GenerateSuccess(string text)
    {
        InitNotification(CreateNotification.NotificationType.Success, text);
    }
}
