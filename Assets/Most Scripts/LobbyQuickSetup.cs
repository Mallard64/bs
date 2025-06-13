using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mirror;

/// <summary>
/// Editor script to quickly setup basic lobby components
/// Use this to create basic lobby structure automatically
/// </summary>
public class LobbyQuickSetup : MonoBehaviour
{
    [Header("Quick Setup")]
    public bool setupLobbyManager = true;
    public bool setupBasicPortals = true;
    public bool setupNotificationSystem = true;
    public bool setupWeaponDisplay = true;
    public bool setupTrainingDummies = true;
    public bool setupLobbyEnvironment = true;
    public bool setupLeaderboard = true;
    public bool setupMusicPlayer = true;
    
    [Header("Prefab References")]
    public GameObject playerPrefab;
    public GameObject[] weaponPrefabs;
    
    [ContextMenu("Quick Setup Lobby")]
    public void QuickSetupLobby()
    {
        Debug.Log("🏛️ Starting comprehensive lobby quick setup...");
        
        if (setupLobbyManager)
            CreateLobbyManager();
            
        if (setupBasicPortals)
            CreateBasicPortals();
            
        if (setupNotificationSystem)
            CreateNotificationSystem();
            
        if (setupWeaponDisplay)
            CreateWeaponDisplay();
            
        if (setupTrainingDummies)
            CreateTrainingDummies();
            
        if (setupLobbyEnvironment)
            CreateLobbyEnvironment();
            
        if (setupLeaderboard)
            CreateLeaderboard();
            
        if (setupMusicPlayer)
            CreateMusicPlayer();
            
        Debug.Log("✅ Comprehensive lobby setup complete!");
    }
    
    void CreateLobbyManager()
    {
        GameObject lobbyManagerObj = new GameObject("LobbyManager");
        
        // Add NetworkIdentity for networking
        lobbyManagerObj.AddComponent<NetworkIdentity>();
        
        var lobbyManager = lobbyManagerObj.AddComponent<LobbyManager>();
        
        // Create spawn points
        GameObject spawnParent = new GameObject("SpawnPoints");
        spawnParent.transform.SetParent(lobbyManagerObj.transform);
        
        Transform[] spawnPoints = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
            spawnPoint.transform.SetParent(spawnParent.transform);
            spawnPoint.transform.position = new Vector3(i * 4, 0, 0); // More spacing
            spawnPoints[i] = spawnPoint.transform;
        }
        
        // Set spawn points directly (works in both editor and runtime)
        lobbyManager.playerSpawnPoints = spawnPoints;
        
        Debug.Log("📍 Created LobbyManager with NetworkIdentity and 4 spawn points");
    }
    
    void CreateBasicPortals()
    {
        // Boss Portal
        CreatePortal("BossPortal", "Boss", "Boss Arena", new Vector3(-5, 0, 0), Color.red);
        
        // Knockout Portal  
        CreatePortal("KnockoutPortal", "Knockout", "PvP Arena", new Vector3(5, 0, 0), Color.blue);
        
        Debug.Log("🌀 Created Boss and Knockout portals");
    }
    
    void CreatePortal(string name, string targetScene, string portalName, Vector3 position, Color portalColor)
    {
        GameObject portalObj = new GameObject(name);
        portalObj.transform.position = position;
        
        // Add NetworkIdentity for networking
        portalObj.AddComponent<NetworkIdentity>();
        
        // Add Portal component
        var portal = portalObj.AddComponent<Portal>();
        portal.targetScene = targetScene;
        portal.portalName = portalName;
        portal.minPlayersRequired = 1;
        portal.maxPlayersAllowed = 4;
        
        // Add visual representation
        var spriteRenderer = portalObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = portalColor;
        spriteRenderer.sprite = CreateSimpleSprite();
        spriteRenderer.sortingOrder = 1;
        
        // Add trigger collider
        var collider = portalObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 2f; // Larger radius for easier interaction
        
        // Add particle system
        var particles = portalObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = portalColor;
        main.startSize = 0.3f;
        main.startLifetime = 3f;
        main.maxParticles = 100;
        
        var emission = particles.emission;
        emission.rateOverTime = 30f;
        
        // Add audio source
        portalObj.AddComponent<AudioSource>();
        
        // Create basic UI
        CreatePortalUI(portalObj, portal);
    }
    
    void CreatePortalUI(GameObject portalObj, Portal portal)
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("PortalUI");
        canvasObj.transform.SetParent(portalObj.transform);
        canvasObj.transform.localPosition = Vector3.up * 2.5f;
        
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        
        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasScaler.scaleFactor = 1f;
        canvasScaler.dynamicPixelsPerUnit = 10f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Set proper canvas size and scale - MUCH larger scale to prevent squishing
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 200);
        canvasObj.transform.localScale = Vector3.one * 0.02f; // Larger scale to be readable
        
        // Create background panel
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform);
        
        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create main title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform);
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = portal.portalName;
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.6f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = new Vector2(20, 10);
        titleRect.offsetMax = new Vector2(-20, -10);
        
        // Create interaction prompt
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
        
        // Initially hide the prompt
        promptObj.SetActive(false);
    }
    
    void CreateNotificationSystem()
    {
        GameObject notificationObj = new GameObject("NotificationSystem");
        
        // Add NetworkIdentity
        notificationObj.AddComponent<NetworkIdentity>();
        
        var notificationSystem = notificationObj.AddComponent<NotificationSystem>();
        
        // Create notification parent UI
        GameObject canvasObj = new GameObject("NotificationCanvas");
        canvasObj.transform.SetParent(notificationObj.transform);
        
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        
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
        
        // Add VerticalLayoutGroup for automatic stacking
        var layoutGroup = notificationParent.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childAlignment = TextAnchor.UpperRight;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        
        // Add ContentSizeFitter for dynamic sizing
        var sizeFitter = notificationParent.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Create a simple notification prefab
        CreateNotificationPrefab(notificationSystem, notificationParent);
        
        // Assign to notification system
        notificationSystem.notificationParent = notificationParent.transform;
        
        Debug.Log("🔔 Created NotificationSystem with proper UI scaling");
    }
    
    void CreateNotificationPrefab(NotificationSystem notificationSystem, GameObject parent)
    {
        // Create notification prefab
        GameObject notificationPrefab = new GameObject("NotificationPrefab");
        
        // Add Image for background
        var bgImage = notificationPrefab.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        bgImage.sprite = CreateRoundedRectSprite();
        
        var rectTransform = notificationPrefab.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(350, 80);
        
        // Add LayoutElement for layout group
        var layoutElement = notificationPrefab.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 80;
        layoutElement.preferredWidth = 350;
        
        // Add notification UI component
        var notificationUI = notificationPrefab.AddComponent<NotificationUI>();
        
        // Create text
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(notificationPrefab.transform);
        
        var messageText = textObj.AddComponent<TextMeshProUGUI>();
        messageText.text = "Notification message";
        messageText.fontSize = 16;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Left;
        messageText.margin = new Vector4(15, 10, 15, 10);
        
        var textRect = messageText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Assign components
        notificationUI.messageText = messageText;
        notificationUI.backgroundImage = bgImage;
        
        // Save as prefab reference (in real implementation, save as .prefab asset)
        notificationSystem.notificationPrefab = notificationPrefab;
        
        // Deactivate the template
        notificationPrefab.SetActive(false);
    }
    
    Sprite CreateRoundedRectSprite()
    {
        // Create a simple rounded rectangle sprite
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // Create rounded corners
                float cornerRadius = 6f;
                Vector2 pos = new Vector2(x, y);
                
                bool isInside = true;
                
                // Check corners
                Vector2[] corners = {
                    new Vector2(cornerRadius, cornerRadius),
                    new Vector2(size - cornerRadius, cornerRadius),
                    new Vector2(cornerRadius, size - cornerRadius),
                    new Vector2(size - cornerRadius, size - cornerRadius)
                };
                
                foreach (var corner in corners)
                {
                    if ((pos.x < cornerRadius && pos.y < cornerRadius && Vector2.Distance(pos, corners[0]) > cornerRadius) ||
                        (pos.x > size - cornerRadius && pos.y < cornerRadius && Vector2.Distance(pos, corners[1]) > cornerRadius) ||
                        (pos.x < cornerRadius && pos.y > size - cornerRadius && Vector2.Distance(pos, corners[2]) > cornerRadius) ||
                        (pos.x > size - cornerRadius && pos.y > size - cornerRadius && Vector2.Distance(pos, corners[3]) > cornerRadius))
                    {
                        isInside = false;
                        break;
                    }
                }
                
                pixels[y * size + x] = isInside ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
    }
    
    void CreateWeaponDisplay()
    {
        GameObject weaponDisplayObj = new GameObject("WeaponDisplay");
        weaponDisplayObj.transform.position = new Vector3(0, 0, -3);
        
        weaponDisplayObj.AddComponent<NetworkIdentity>();
        var weaponDisplay = weaponDisplayObj.AddComponent<WeaponDisplay>();
        
        // Create display pedestal
        GameObject pedestal = new GameObject("Pedestal");
        pedestal.transform.SetParent(weaponDisplayObj.transform);
        pedestal.transform.localPosition = Vector3.zero;
        
        var pedestalRenderer = pedestal.AddComponent<SpriteRenderer>();
        pedestalRenderer.sprite = CreateSimpleSprite();
        pedestalRenderer.color = new Color(0.3f, 0.3f, 0.3f);
        pedestalRenderer.sortingOrder = -1;
        
        // Create weapon display point
        GameObject weaponPoint = new GameObject("WeaponDisplayPoint");
        weaponPoint.transform.SetParent(weaponDisplayObj.transform);
        weaponPoint.transform.localPosition = Vector3.up * 0.5f;
        
        weaponDisplay.displayPoint = weaponPoint.transform;
        
        // Set weapon prefabs if available
        if (weaponPrefabs != null && weaponPrefabs.Length > 0)
        {
            weaponDisplay.weaponPrefabs = weaponPrefabs;
        }
        
        Debug.Log("⚔️ Created WeaponDisplay with pedestal");
    }
    
    void CreateTrainingDummies()
    {
        // Create multiple training dummies around the lobby
        Vector3[] dummyPositions = {
            new Vector3(-10, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(0, 0, 5),
            new Vector3(0, 0, -5)
        };
        
        for (int i = 0; i < dummyPositions.Length; i++)
        {
            GameObject dummy = CreateTrainingDummy($"TrainingDummy_{i + 1}", dummyPositions[i]);
        }
        
        Debug.Log("🎯 Created 4 training dummies");
    }
    
    GameObject CreateTrainingDummy(string name, Vector3 position)
    {
        GameObject dummyObj = new GameObject(name);
        dummyObj.transform.position = position;
        
        dummyObj.AddComponent<NetworkIdentity>();
        var trainingDummy = dummyObj.AddComponent<TrainingDummy>();
        
        // Add visual representation
        var spriteRenderer = dummyObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateSimpleSprite();
        spriteRenderer.color = new Color(0.8f, 0.4f, 0.2f);
        spriteRenderer.sortingOrder = 1;
        
        // Add collider for interaction
        var collider = dummyObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 1.5f;
        
        // Add health system
        var hittable = dummyObj.AddComponent<Hittable>();
        hittable.maxHealth = 100;
        hittable.currentHealth = 100;
        
        // Create simple UI for dummy
        CreateDummyUI(dummyObj, trainingDummy);
        
        return dummyObj;
    }
    
    void CreateDummyUI(GameObject dummyObj, TrainingDummy dummy)
    {
        GameObject canvasObj = new GameObject("DummyUI");
        canvasObj.transform.SetParent(dummyObj.transform);
        canvasObj.transform.localPosition = Vector3.up * 2f;
        
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 100);
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        // Create interaction text
        GameObject textObj = new GameObject("InteractionText");
        textObj.transform.SetParent(canvasObj.transform);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Training Dummy\nPress F to interact";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        dummy.dummyUI = canvas;
        
        canvasObj.SetActive(false);
    }
    
    void CreateLobbyEnvironment()
    {
        // Create lobby decorations and environment
        GameObject environmentParent = new GameObject("LobbyEnvironment");
        
        // Create some walls/boundaries
        CreateWalls(environmentParent.transform);
        
        // Create decorative elements
        CreateDecorations(environmentParent.transform);
        
        // Create ambient lighting
        CreateAmbientLighting(environmentParent.transform);
        
        Debug.Log("🌿 Created lobby environment with walls and decorations");
    }
    
    void CreateWalls(Transform parent)
    {
        Vector3[] wallPositions = {
            new Vector3(-15, 0, 0),  // Left wall
            new Vector3(15, 0, 0),   // Right wall
            new Vector3(0, 0, 10),   // Back wall
            new Vector3(0, 0, -10)   // Front wall
        };
        
        Vector3[] wallScales = {
            new Vector3(1, 5, 20),   // Left wall
            new Vector3(1, 5, 20),   // Right wall
            new Vector3(30, 5, 1),   // Back wall
            new Vector3(30, 5, 1)    // Front wall
        };
        
        for (int i = 0; i < wallPositions.Length; i++)
        {
            GameObject wall = new GameObject($"Wall_{i + 1}");
            wall.transform.SetParent(parent);
            wall.transform.position = wallPositions[i];
            wall.transform.localScale = wallScales[i];
            
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSimpleSprite();
            renderer.color = new Color(0.4f, 0.4f, 0.4f);
            renderer.sortingOrder = -2;
            
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
        }
    }
    
    void CreateDecorations(Transform parent)
    {
        // Create some decorative bushes/objects
        Vector3[] decorationPositions = {
            new Vector3(-8, 0, 8),
            new Vector3(8, 0, 8),
            new Vector3(-8, 0, -8),
            new Vector3(8, 0, -8),
            new Vector3(0, 0, 0)  // Center piece
        };
        
        for (int i = 0; i < decorationPositions.Length; i++)
        {
            GameObject decoration = new GameObject($"Decoration_{i + 1}");
            decoration.transform.SetParent(parent);
            decoration.transform.position = decorationPositions[i];
            
            var renderer = decoration.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSimpleSprite();
            renderer.color = new Color(0.2f, 0.6f, 0.2f);
            renderer.sortingOrder = 0;
            
            // Add some particle effects
            var particles = decoration.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(0.8f, 1f, 0.8f, 0.3f);
            main.startSize = 0.1f;
            main.startLifetime = 2f;
            main.maxParticles = 20;
            
            var emission = particles.emission;
            emission.rateOverTime = 5f;
        }
    }
    
    void CreateAmbientLighting(Transform parent)
    {
        GameObject lightObj = new GameObject("AmbientLight");
        lightObj.transform.SetParent(parent);
        lightObj.transform.position = new Vector3(0, 5, 0);
        
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.8f);
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
    }
    
    void CreateLeaderboard()
    {
        GameObject leaderboardObj = new GameObject("Leaderboard");
        leaderboardObj.transform.position = new Vector3(-12, 0, 5);
        
        // Create leaderboard UI
        GameObject canvasObj = new GameObject("LeaderboardCanvas");
        canvasObj.transform.SetParent(leaderboardObj.transform);
        canvasObj.transform.localPosition = Vector3.up * 2f;
        
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;
        
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600, 800);
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        // Create background
        GameObject panelObj = new GameObject("LeaderboardPanel");
        panelObj.transform.SetParent(canvasObj.transform);
        
        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create title
        GameObject titleObj = new GameObject("LeaderboardTitle");
        titleObj.transform.SetParent(panelObj.transform);
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "🏆 LEADERBOARD 🏆";
        titleText.fontSize = 36;
        titleText.color = Color.yellow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // Create content area
        GameObject contentObj = new GameObject("LeaderboardContent");
        contentObj.transform.SetParent(panelObj.transform);
        
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 0.9f);
        contentRect.offsetMin = new Vector2(20, 20);
        contentRect.offsetMax = new Vector2(-20, -20);
        
        var layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        
        // Update LobbyManager reference if it exists
        var lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.leaderboardPanel = panelObj;
            lobbyManager.leaderboardContent = contentObj.transform;
        }
        
        Debug.Log("🏆 Created leaderboard display");
    }
    
    void CreateMusicPlayer()
    {
        GameObject musicPlayerObj = new GameObject("LobbyMusicPlayer");
        musicPlayerObj.AddComponent<NetworkIdentity>();
        
        var audioSource = musicPlayerObj.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.3f;
        audioSource.spatialBlend = 0f; // 2D sound
        
        var musicPlayer = musicPlayerObj.AddComponent<MusicPlayer>();
        
        // Update LobbyManager reference if it exists
        var lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.musicPlayer = musicPlayer;
        }
        
        Debug.Log("🎵 Created lobby music player");
    }
    
    Sprite CreateSimpleSprite()
    {
        // Create a simple white circle sprite
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        Vector2 center = new Vector2(32, 32);
        float radius = 30;
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius);
                    pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
}