using UnityEngine;
using Mirror;

/// <summary>
/// Helper script to automatically add NetworkIdentity components to lobby objects
/// This ensures all networked lobby components work properly with Mirror
/// </summary>
public class LobbyNetworkHelper : MonoBehaviour
{
    [Header("Auto-Setup Network Components")]
    public bool autoAddNetworkIdentity = true;
    public bool serverOnly = false;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    void Awake()
    {
        if (autoAddNetworkIdentity)
        {
            EnsureNetworkIdentity();
        }
    }
    
    [ContextMenu("Add Network Identity")]
    public void EnsureNetworkIdentity()
    {
        var networkIdentity = GetComponent<NetworkIdentity>();
        
        if (networkIdentity == null)
        {
            networkIdentity = gameObject.AddComponent<NetworkIdentity>();
            
            if (showDebugLogs)
            {
                Debug.Log($"🌐 Added NetworkIdentity to {gameObject.name}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"🌐 NetworkIdentity already exists on {gameObject.name}");
            }
        }
    }
    
    [ContextMenu("Scan and Fix All Lobby Objects")]
    public void ScanAndFixAllLobbyObjects()
    {
        // Find all lobby-related components and ensure they have NetworkIdentity
        var lobbyComponents = new System.Type[]
        {
            typeof(LobbyManager),
            typeof(Portal),
            typeof(NotificationSystem),
            typeof(WeaponDisplay),
            typeof(TrainingDummy)
        };
        
        int fixedCount = 0;
        
        foreach (var componentType in lobbyComponents)
        {
            var objects = FindObjectsOfType(componentType);
            foreach (var obj in objects)
            {
                var go = ((Component)obj).gameObject;
                var networkId = go.GetComponent<NetworkIdentity>();
                
                if (networkId == null)
                {
                    networkId = go.AddComponent<NetworkIdentity>();
                    
                    fixedCount++;
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"🔧 Fixed NetworkIdentity on {go.name} ({componentType.Name})");
                    }
                }
            }
        }
        
        Debug.Log($"✅ Scanned and fixed {fixedCount} lobby objects with missing NetworkIdentity components");
    }
    
    [ContextMenu("Fix UI Scaling Issues")]
    public void FixUIScalingIssues()
    {
        // Find all world space canvases and fix their scaling
        var worldCanvases = FindObjectsOfType<Canvas>();
        int fixedCount = 0;
        
        foreach (var canvas in worldCanvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                // Check if scale is too small (squished)
                if (canvas.transform.localScale.x < 0.01f)
                {
                    canvas.transform.localScale = Vector3.one * 0.02f; // Better scale
                    
                    // Also fix canvas size
                    var rectTransform = canvas.GetComponent<RectTransform>();
                    if (rectTransform != null && rectTransform.sizeDelta.x < 200)
                    {
                        rectTransform.sizeDelta = new Vector2(400, 200);
                    }
                    
                    fixedCount++;
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"🔧 Fixed squished UI scaling on {canvas.gameObject.name}");
                    }
                }
            }
        }
        
        Debug.Log($"✅ Fixed {fixedCount} squished UI elements");
    }
    
    [ContextMenu("Fix All Lobby Issues")]
    public void FixAllLobbyIssues()
    {
        ScanAndFixAllLobbyObjects();
        FixUIScalingIssues();
        
        Debug.Log("🚀 All lobby issues fixed!");
    }
}