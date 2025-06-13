using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Simple lobby setup without editor dependencies
/// Attach to empty GameObject and call methods manually
/// </summary>
public class SimpleLobbySetup : MonoBehaviour
{
    [Header("Setup Options")]
    public bool createLobbyManager = true;
    public bool createPortals = true;
    public bool createNotifications = true;
    
    [Header("Portal Settings")]
    public string bossSceneName = "Boss";
    public string knockoutSceneName = "Knockout";
    
    void Start()
    {
        // Don't auto-run, let user call manually
    }
    
    [ContextMenu("Setup Lobby")]
    public void SetupLobby()
    {
        Debug.Log("🏛️ Starting simple lobby setup...");
        
        if (createLobbyManager)
            CreateLobbyManager();
            
        if (createPortals)
            CreatePortals();
            
        if (createNotifications)
            CreateNotificationSystem();
            
        Debug.Log("✅ Simple lobby setup complete!");
    }
    
    void CreateLobbyManager()
    {
        if (FindObjectOfType<LobbyManager>() != null)
        {
            Debug.Log("📍 LobbyManager already exists, skipping...");
            return;
        }
        
        GameObject lobbyManagerObj = new GameObject("LobbyManager");
        
        // Add NetworkIdentity
        lobbyManagerObj.AddComponent<NetworkIdentity>();
        
        var lobbyManager = lobbyManagerObj.AddComponent<LobbyManager>();
        
        // Create spawn points
        Transform[] spawnPoints = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
            spawnPoint.transform.SetParent(lobbyManagerObj.transform);
            spawnPoint.transform.position = new Vector3(i * 4, 0, 0);
            spawnPoints[i] = spawnPoint.transform;
        }
        
        // Set spawn points directly (no editor code)
        lobbyManager.playerSpawnPoints = spawnPoints;
        
        Debug.Log("📍 Created LobbyManager with 4 spawn points");
    }
    
    void CreatePortals()
    {
        if (FindObjectOfType<Portal>() != null)
        {
            Debug.Log("🌀 Portals already exist, skipping...");
            return;
        }
        
        // Boss Portal
        CreatePortal("BossPortal", bossSceneName, "Boss Arena", new Vector3(-5, 0, 0), Color.red);
        
        // Knockout Portal
        CreatePortal("KnockoutPortal", knockoutSceneName, "PvP Arena", new Vector3(5, 0, 0), Color.blue);
        
        Debug.Log("🌀 Created Boss and Knockout portals");
    }
    
    void CreatePortal(string name, string targetScene, string portalName, Vector3 position, Color portalColor)
    {
        GameObject portalObj = new GameObject(name);
        portalObj.transform.position = position;
        
        // Add NetworkIdentity
        portalObj.AddComponent<NetworkIdentity>();
        
        // Add Portal component
        var portal = portalObj.AddComponent<Portal>();
        portal.targetScene = targetScene;
        portal.portalName = portalName;
        portal.minPlayersRequired = 1;
        portal.maxPlayersAllowed = 4;
        
        // Add visual components
        var spriteRenderer = portalObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = portalColor;
        
        var collider = portalObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 2f;
        
        portalObj.AddComponent<AudioSource>();
        
        // Create simple UI
        CreateSimplePortalUI(portalObj, portal, portalName);
    }
    
    void CreateSimplePortalUI(GameObject portalObj, Portal portal, string portalName)
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("PortalUI");
        canvasObj.transform.SetParent(portalObj.transform);
        canvasObj.transform.localPosition = Vector3.up * 2.5f;
        canvasObj.transform.localScale = Vector3.one * 0.02f;
        
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 200);
        
        // Create background
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform);
        
        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform);
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = portalName;
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.6f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = new Vector2(20, 10);
        titleRect.offsetMax = new Vector2(-20, -10);
        
        // Create prompt text
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(panelObj.transform);
        
        var promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "Press E to enter";
        promptText.fontSize = 22;
        promptText.color = Color.green;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontStyle = FontStyles.Bold;
        
        var promptRect = promptText.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0, 0.3f);
        promptRect.anchorMax = new Vector2(1, 0.6f);
        promptRect.offsetMin = new Vector2(20, 5);
        promptRect.offsetMax = new Vector2(-20, -5);
        
        // Create player count text
        GameObject countObj = new GameObject("PlayerCountText");
        countObj.transform.SetParent(panelObj.transform);
        
        var countText = countObj.AddComponent<TextMeshProUGUI>();
        countText.text = "0/4 Players";
        countText.fontSize = 18;
        countText.color = Color.yellow;
        countText.alignment = TextAlignmentOptions.Center;
        
        var countRect = countText.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0, 0);
        countRect.anchorMax = new Vector2(1, 0.3f);
        countRect.offsetMin = new Vector2(20, 5);
        countRect.offsetMax = new Vector2(-20, -5);
        
        // Assign to portal
        portal.interactionPrompt = promptObj;
        portal.promptText = promptText;
        portal.playerCountText = countText;
        portal.portalUI = canvas;
        
        promptObj.SetActive(false);
    }
    
    void CreateNotificationSystem()
    {
        if (FindObjectOfType<NotificationSystem>() != null)
        {
            Debug.Log("🔔 NotificationSystem already exists, skipping...");
            return;
        }
        
        GameObject notificationObj = new GameObject("NotificationSystem");
        
        notificationObj.AddComponent<NetworkIdentity>();
        
        var notificationSystem = notificationObj.AddComponent<NotificationSystem>();
        
        // Create simple notification UI
        GameObject canvasObj = new GameObject("NotificationCanvas");
        canvasObj.transform.SetParent(notificationObj.transform);
        
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create notification parent
        GameObject notificationParent = new GameObject("NotificationParent");
        notificationParent.transform.SetParent(canvasObj.transform);
        
        var rectTransform = notificationParent.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-30, -30);
        rectTransform.sizeDelta = new Vector2(400, 600);
        
        var layoutGroup = notificationParent.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childAlignment = TextAnchor.UpperRight;
        
        notificationSystem.notificationParent = notificationParent.transform;
        
        Debug.Log("🔔 Created NotificationSystem");
    }
    
    [ContextMenu("Add Network Components")]
    public void AddNetworkComponents()
    {
        // Find all lobby objects and add NetworkIdentity
        var lobbyObjects = new Component[]
        {
            FindObjectOfType<LobbyManager>(),
            FindObjectOfType<Portal>(),
            FindObjectOfType<NotificationSystem>()
        };
        
        foreach (var obj in lobbyObjects)
        {
            if (obj != null && obj.GetComponent<NetworkIdentity>() == null)
            {
                obj.gameObject.AddComponent<NetworkIdentity>();
                Debug.Log($"🌐 Added NetworkIdentity to {obj.gameObject.name}");
            }
        }
    }
    
    [ContextMenu("Fix UI Scaling")]
    public void FixUIScaling()
    {
        var canvases = FindObjectsOfType<Canvas>();
        
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.transform.localScale.x < 0.01f)
            {
                canvas.transform.localScale = Vector3.one * 0.02f;
                
                var rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(400, 200);
                }
                
                Debug.Log($"🔧 Fixed scaling on {canvas.gameObject.name}");
            }
        }
    }
}