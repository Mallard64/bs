using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Helper script to fix and adjust lobby UI positioning
/// Attach this to any UI element that needs positioning fixes
/// </summary>
public class LobbyUIHelper : MonoBehaviour
{
    [Header("UI Positioning")]
    public bool fixWorldSpaceCanvas = true;
    public bool fixTextAlignment = true;
    public bool fixNotificationPositioning = true;
    
    [Header("World Space Canvas Settings")]
    [Range(0.01f, 0.05f)]
    public float worldCanvasScale = 0.02f;
    public Vector3 worldCanvasOffset = Vector3.up * 2.5f;
    
    [Header("Text Settings")]
    public float textFontSize = 36f;
    public Color textColor = Color.white;
    public TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
    
    [Header("Notification Settings")]
    public Vector2 notificationSize = new Vector2(350, 80);
    public Vector2 notificationPosition = new Vector2(-30, -30);
    
    void Start()
    {
        if (fixWorldSpaceCanvas)
            FixWorldSpaceCanvas();
            
        if (fixTextAlignment)
            FixTextElements();
            
        if (fixNotificationPositioning)
            FixNotificationPositioning();
    }
    
    [ContextMenu("Fix World Space Canvas")]
    public void FixWorldSpaceCanvas()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            // Fix canvas scale
            transform.localScale = Vector3.one * worldCanvasScale;
            
            // Fix canvas position
            transform.localPosition = worldCanvasOffset;
            
            // Fix canvas scaler
            var canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                canvasScaler.scaleFactor = 1f;
            }
            
            // Set canvas size
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(400, 200);
            }
            
            Debug.Log($"✅ Fixed world space canvas: {gameObject.name}");
        }
    }
    
    [ContextMenu("Fix Text Elements")]
    public void FixTextElements()
    {
        var textComponents = GetComponentsInChildren<TextMeshProUGUI>();
        
        foreach (var text in textComponents)
        {
            text.fontSize = textFontSize;
            text.color = textColor;
            text.alignment = textAlignment;
            text.fontStyle = FontStyles.Bold;
            
            // Fix text rect transform
            var rectTransform = text.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = new Vector2(10, 10);
                rectTransform.offsetMax = new Vector2(-10, -10);
            }
        }
        
        Debug.Log($"✅ Fixed {textComponents.Length} text elements in: {gameObject.name}");
    }
    
    [ContextMenu("Fix Notification Positioning")]
    public void FixNotificationPositioning()
    {
        // Check if this is a notification system
        var notificationSystem = GetComponent<NotificationSystem>();
        if (notificationSystem != null)
        {
            // Find the notification parent
            var notificationParent = GetComponentInChildren<VerticalLayoutGroup>();
            if (notificationParent != null)
            {
                var rectTransform = notificationParent.GetComponent<RectTransform>();
                
                // Fix anchoring to top-right
                rectTransform.anchorMin = new Vector2(1, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(1, 1);
                rectTransform.anchoredPosition = notificationPosition;
                rectTransform.sizeDelta = new Vector2(400, 600);
                
                Debug.Log($"✅ Fixed notification positioning: {gameObject.name}");
            }
        }
        
        // Check if this is a notification prefab
        var notificationUI = GetComponent<NotificationUI>();
        if (notificationUI != null)
        {
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = notificationSize;
            
            // Fix layout element
            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = notificationSize.y;
                layoutElement.preferredWidth = notificationSize.x;
            }
            
            Debug.Log($"✅ Fixed notification prefab sizing: {gameObject.name}");
        }
    }
    
    [ContextMenu("Fix Portal UI")]
    public void FixPortalUI()
    {
        var portal = GetComponent<Portal>();
        if (portal != null)
        {
            // Fix portal canvas
            if (portal.portalUI != null)
            {
                var canvas = portal.portalUI.GetComponent<Canvas>();
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.transform.localScale = Vector3.one * worldCanvasScale;
                    canvas.transform.localPosition = worldCanvasOffset;
                }
            }
            
            // Fix prompt text
            if (portal.promptText != null)
            {
                portal.promptText.fontSize = textFontSize;
                portal.promptText.color = textColor;
                portal.promptText.alignment = TextAlignmentOptions.Center;
                portal.promptText.fontStyle = FontStyles.Bold;
            }
            
            // Fix player count text
            if (portal.playerCountText != null)
            {
                portal.playerCountText.fontSize = textFontSize * 0.7f;
                portal.playerCountText.color = Color.yellow;
                portal.playerCountText.alignment = TextAlignmentOptions.Center;
            }
            
            Debug.Log($"✅ Fixed portal UI: {gameObject.name}");
        }
    }
    
    [ContextMenu("Fix All UI")]
    public void FixAllUI()
    {
        FixWorldSpaceCanvas();
        FixTextElements();
        FixNotificationPositioning();
        FixPortalUI();
        
        Debug.Log($"🔧 Fixed all UI elements for: {gameObject.name}");
    }
    
    // Gizmo to show UI bounds in scene view
    void OnDrawGizmos()
    {
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(rectTransform.sizeDelta.x * transform.localScale.x, 
                                     rectTransform.sizeDelta.y * transform.localScale.y, 0);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}