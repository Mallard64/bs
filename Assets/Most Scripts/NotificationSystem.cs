using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public class NotificationSystem : MonoBehaviour
{
    [Header("Notification Settings")]
    public GameObject notificationPrefab;
    public Transform notificationParent;
    public float notificationDuration = 3f;
    public float slideInTime = 0.5f;
    public float slideOutTime = 0.3f;
    public int maxNotifications = 5;
    
    [Header("Notification Types")]
    public Color infoColor = Color.white;
    public Color successColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;
    
    private Queue<GameObject> activeNotifications = new Queue<GameObject>();
    private static NotificationSystem _instance;
    public static NotificationSystem Instance => _instance;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void ShowNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info)
    {
        if (duration < 0) duration = notificationDuration;
        
        StartCoroutine(CreateNotification(message, duration, type));
    }
    
    IEnumerator CreateNotification(string message, float duration, NotificationType type)
    {
        // Remove oldest notification if at max capacity
        if (activeNotifications.Count >= maxNotifications)
        {
            var oldest = activeNotifications.Dequeue();
            if (oldest != null)
            {
                StartCoroutine(SlideOutNotification(oldest, true));
            }
        }
        
        // Create new notification
        GameObject notification = Instantiate(notificationPrefab, notificationParent);
        activeNotifications.Enqueue(notification);
        
        // Setup notification
        var notificationUI = notification.GetComponent<NotificationUI>();
        if (notificationUI != null)
        {
            notificationUI.Setup(message, GetColorForType(type), type);
        }
        
        // Slide in animation
        yield return StartCoroutine(SlideInNotification(notification));
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Slide out animation
        yield return StartCoroutine(SlideOutNotification(notification, false));
        
        // Remove from queue and destroy
        if (activeNotifications.Contains(notification))
        {
            var tempQueue = new Queue<GameObject>();
            while (activeNotifications.Count > 0)
            {
                var item = activeNotifications.Dequeue();
                if (item != notification)
                {
                    tempQueue.Enqueue(item);
                }
            }
            activeNotifications = tempQueue;
        }
        
        Destroy(notification);
    }
    
    IEnumerator SlideInNotification(GameObject notification)
    {
        var rectTransform = notification.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector3 targetPosition = rectTransform.localPosition;
        Vector3 startPosition = targetPosition + Vector3.right * 400f; // Start off-screen
        
        rectTransform.localPosition = startPosition;
        
        float timer = 0f;
        while (timer < slideInTime)
        {
            timer += Time.deltaTime;
            float progress = timer / slideInTime;
            progress = Mathf.SmoothStep(0f, 1f, progress); // Smooth ease
            
            rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }
        
        rectTransform.localPosition = targetPosition;
    }
    
    IEnumerator SlideOutNotification(GameObject notification, bool slideUp)
    {
        var rectTransform = notification.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector3 startPosition = rectTransform.localPosition;
        Vector3 targetPosition;
        
        if (slideUp)
        {
            targetPosition = startPosition + Vector3.up * 100f; // Slide up when making room
        }
        else
        {
            targetPosition = startPosition + Vector3.right * 400f; // Slide right when expiring
        }
        
        float timer = 0f;
        while (timer < slideOutTime)
        {
            timer += Time.deltaTime;
            float progress = timer / slideOutTime;
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // Fade out
            var canvasGroup = notification.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            }
            
            yield return null;
        }
    }
    
    Color GetColorForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success: return successColor;
            case NotificationType.Warning: return warningColor;
            case NotificationType.Error: return errorColor;
            default: return infoColor;
        }
    }
    
    public void ClearAllNotifications()
    {
        StopAllCoroutines();
        
        while (activeNotifications.Count > 0)
        {
            var notification = activeNotifications.Dequeue();
            if (notification != null)
            {
                Destroy(notification);
            }
        }
    }
}

// Simple notification UI component
public class NotificationUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI messageText;
    public UnityEngine.UI.Image backgroundImage;
    public UnityEngine.UI.Image iconImage;
    
    [Header("Icons")]
    public Sprite infoIcon;
    public Sprite successIcon;
    public Sprite warningIcon;
    public Sprite errorIcon;
    
    public void Setup(string message, Color color, NotificationType type)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = color;
            bgColor.a = 0.2f;
            backgroundImage.color = bgColor;
        }
        
        if (iconImage != null)
        {
            iconImage.sprite = GetIconForType(type);
            iconImage.color = color;
        }
    }
    
    Sprite GetIconForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success: return successIcon;
            case NotificationType.Warning: return warningIcon;
            case NotificationType.Error: return errorIcon;
            default: return infoIcon;
        }
    }
}