using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class CustomNetworkManager1 : NetworkManager
{
    public GameObject warriorPrefab;
    public GameObject background;
    public GameObject goal;
    public GameObject goal1;
    public Vector3 t;
    public List<Vector3> spawnPoints; // All spawn points
    public List<TextMeshProUGUI> texts; // All spawn points
    public Queue<Vector3> availableSpawnPoints = new Queue<Vector3>(); // Queue to cycle through spawn points
    public Dictionary<int, Vector3> playerSpawnPoints = new Dictionary<int, Vector3>();
    public Queue<TextMeshProUGUI> availableText = new Queue<TextMeshProUGUI>(); // Queue to cycle through spawn points
    public Dictionary<int, TextMeshProUGUI> playerText = new Dictionary<int, TextMeshProUGUI>();
    string oldScene = "";
    private int nextSpawnIndex = 0;

    [Tooltip("Pickup prefab variants to spawn")]
    public GameObject[] weaponPickupPrefabs;

    [Tooltip("Time in seconds between spawns")]
    public float spawnInterval = 5f;

    [Header("Weapon Spawn Settings")]
    [Tooltip("Use predefined spawn points instead of random area")]
    public bool useWeaponSpawnPoints = true;

    [Tooltip("Weapon spawn points (assign in inspector or will find by tag)")]
    public List<Vector3> weaponSpawnPoints = new List<Vector3>();

    [Tooltip("Tag to find weapon spawn points automatically")]
    public string weaponSpawnPointTag = "WeaponSpawnPoint";

    private Queue<Vector3> availableWeaponSpawnPoints = new Queue<Vector3>();

    [Header("Fallback Random Spawn Area")]
    [Tooltip("Minimum corner of spawn area (world coords) - used if no weapon spawn points")]
    public Vector2 spawnAreaMin;

    [Tooltip("Maximum corner of spawn area (world coords) - used if no weapon spawn points")]
    public Vector2 spawnAreaMax;

    public GameObject weaponPrefab;

    [Header("Indicator")]
    public GameObject spawnIndicatorPrefab; // assign in inspector
    public float indicatorDuration = 1f;    // how long the indicator shows

    [Header("Auto-Restart Settings")]
    [Tooltip("Enable auto-restart when no players are connected")]
    public bool enableAutoRestart = true;

    [Tooltip("Time to wait before restarting after last player leaves")]
    public float restartDelay = 5f;

    private bool restartPending = false;
    private Coroutine restartCoroutine;

    // Store original spawn points and texts for restoration
    private List<Vector3> originalSpawnPoints = new List<Vector3>();
    private List<Vector3> originalWeaponSpawnPoints = new List<Vector3>();
    private List<TextMeshProUGUI> originalTexts = new List<TextMeshProUGUI>();

    //public Player localPlayer;
    private string m_SessionId = "";
    private string m_Username;
    private string m_UserId;

    /// <summary>
    /// Flag to determine if the user is logged into the backend.
    /// </summary>
    public bool isLoggedIn = false;

    /// <summary>
    /// List of players currently connected to the server.
    /// </summary>
    //private List<Player> m_Players;

    public override void Awake()
    {
        base.Awake();
        Debug.Log("Auto-connecting as Client to: " + networkAddress);
    }

    public async void UnityLogin()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Logged into Unity, player ID: " + AuthenticationService.Instance.PlayerId);
            isLoggedIn = true;
        }
        catch (Exception e)
        {
            isLoggedIn = false;
            Debug.Log(e);
        }
    }

    public override void Start()
    {
        base.Start();

        if (PlayerPrefs.GetInt("port") != 0)
        {
            GetComponent<TelepathyTransport>().port = (ushort)PlayerPrefs.GetInt("port");
        }

        // Delay initialization to ensure scene is fully loaded
        StartCoroutine(DelayedInitialization());

        // Only start client if NOT in headless server mode
        if (!Application.isBatchMode)
        {
            StartClient();
        }
    }

    IEnumerator DelayedInitialization()
    {
        // Wait a frame to ensure all GameObjects are instantiated
        yield return new WaitForEndOfFrame();

        Debug.Log("Starting delayed initialization...");

        // Initialize spawn points and store originals
        InitializeSpawnPoints();

        // Initialize weapon spawn points
        InitializeWeaponSpawnPoints();

        // Force refresh if still empty
        if (originalSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found on first try, attempting manual refresh...");
            yield return new WaitForSeconds(1f);
            RefreshSpawnPoints();
        }
    }

    // Manual method to refresh spawn points - can be called from inspector or code
    public void RefreshSpawnPoints()
    {
        Debug.Log("=== MANUAL SPAWN POINT REFRESH ===");
        InitializeSpawnPoints();
        InitializeWeaponSpawnPoints();
    }

    void InitializeSpawnPoints()
    {
        Debug.Log("=== INITIALIZING SPAWN POINTS ===");

        // Find all SpawnPoint objects in the scene and add their Vector3 to the spawnPoints list
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        Debug.Log($"Found {spawnPointObjects.Length} GameObjects with 'SpawnPoint' tag");

        // Clear existing points
        spawnPoints.Clear();
        originalSpawnPoints.Clear();

        foreach (GameObject obj in spawnPointObjects)
        {
            Vector3 spawnPos = obj.transform.position;
            spawnPoints.Add(spawnPos);
            originalSpawnPoints.Add(spawnPos); // Store original
            Debug.Log($"Added spawn point: {spawnPos} from GameObject: {obj.name}");
        }

        // Store original texts (make sure this is populated before calling)
        originalTexts.Clear();
        originalTexts.AddRange(texts);
        Debug.Log($"Stored {originalTexts.Count} original text objects");

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points found! Make sure spawn points are tagged correctly.");
            Debug.LogError("Available GameObjects in scene:");

            // Debug: List all GameObjects to see what's available
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("spawn"))
                {
                    Debug.LogError($"Found GameObject with 'spawn' in name: {obj.name} - Tag: {obj.tag}");
                }
            }
        }
        else
        {
            // Initialize the queue with all spawn points
            availableSpawnPoints.Clear();
            foreach (var spawnPoint in spawnPoints)
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
            Debug.Log($"Successfully initialized {spawnPoints.Count} player spawn points");
        }
    }

    void InitializeWeaponSpawnPoints()
    {
        // Clear existing weapon spawn points
        weaponSpawnPoints.Clear();
        originalWeaponSpawnPoints.Clear();

        // If no weapon spawn points are manually assigned, find them by tag
        if (!string.IsNullOrEmpty(weaponSpawnPointTag))
        {
            GameObject[] weaponSpawnObjects = GameObject.FindGameObjectsWithTag(weaponSpawnPointTag);
            foreach (GameObject obj in weaponSpawnObjects)
            {
                Vector3 weaponSpawnPos = obj.transform.position;
                weaponSpawnPoints.Add(weaponSpawnPos);
                originalWeaponSpawnPoints.Add(weaponSpawnPos); // Store original
            }
        }

        // Initialize the queue with all weapon spawn points
        availableWeaponSpawnPoints.Clear();
        if (weaponSpawnPoints.Count > 0)
        {
            foreach (var spawnPoint in weaponSpawnPoints)
            {
                availableWeaponSpawnPoints.Enqueue(spawnPoint);
            }
            Debug.Log($"Initialized {weaponSpawnPoints.Count} weapon spawn points.");
        }
        else
        {
            Debug.LogWarning("No weapon spawn points found! Will use random area spawning as fallback.");
            useWeaponSpawnPoints = false;
        }
    }

    Vector3 GetNextWeaponSpawnPoint()
    {
        if (availableWeaponSpawnPoints.Count == 0)
        {
            // Refill the queue when empty (cycles through all spawn points)
            foreach (var spawnPoint in weaponSpawnPoints)
            {
                availableWeaponSpawnPoints.Enqueue(spawnPoint);
            }
        }

        Vector3 nextSpawnPoint = availableWeaponSpawnPoints.Dequeue();
        return nextSpawnPoint;
    }

    // Function to get the next available spawn point
    public Vector3 GetNextSpawnPoint()
    {
        if (availableSpawnPoints.Count == 0)
        {
            Debug.Log("All spawn points used up, re-adding all spawn points to the queue.");
            foreach (var spawnPoint in spawnPoints)
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
        }

        Vector3 nextSpawnPoint = availableSpawnPoints.Dequeue();
        availableSpawnPoints.Enqueue(nextSpawnPoint); // Recycle the used spawn point
        return nextSpawnPoint;
    }

    // Function to get a unique spawn point for each player
    public Vector3 AssignSpawnPoint(int conn)
    {
        Debug.Log($"=== ASSIGNING SPAWN POINT FOR CONNECTION {conn} ===");
        Debug.Log($"Available original spawn points: {originalSpawnPoints.Count}");
        Debug.Log($"Current player assignments: {playerSpawnPoints.Count}");

        // Check if the player already has an assigned spawn point
        if (playerSpawnPoints.ContainsKey(conn))
        {
            Debug.Log($"Player {conn} already has spawn point: {playerSpawnPoints[conn]}");
            return playerSpawnPoints[conn]; // Return the already assigned spawn point
        }
        else if (originalSpawnPoints.Count > 0)
        {
            // Use originalSpawnPoints instead of spawnPoints to avoid permanent removal
            int spawnIndex = playerSpawnPoints.Count % originalSpawnPoints.Count;
            Vector3 spawnPoint = originalSpawnPoints[spawnIndex];
            playerSpawnPoints.Add(conn, spawnPoint); // Assign it to the player

            Debug.Log($"Assigned spawn point {spawnIndex} to player {conn}: {spawnPoint}");
            return spawnPoint;
        }
        else
        {
            Debug.LogError("No original spawn points available! Attempting refresh...");
            RefreshSpawnPoints();

            if (originalSpawnPoints.Count > 0)
            {
                int spawnIndex = playerSpawnPoints.Count % originalSpawnPoints.Count;
                Vector3 spawnPoint = originalSpawnPoints[spawnIndex];
                playerSpawnPoints.Add(conn, spawnPoint);
                Debug.Log($"After refresh - Assigned spawn point {spawnIndex} to player {conn}: {spawnPoint}");
                return spawnPoint;
            }
        }

        Debug.LogError("Still no available spawn points after refresh!");
        return Vector3.zero;
    }

    // Function to get the next available text
    public TextMeshProUGUI GetNextText()
    {
        if (availableText.Count == 0)
        {
            Debug.Log("All text points used up, re-adding all text points to the queue.");
            foreach (var text in texts)
            {
                availableText.Enqueue(text);
            }
        }

        TextMeshProUGUI nextText = availableText.Dequeue();
        availableText.Enqueue(nextText); // Recycle the used text
        return nextText;
    }

    // Function to get a unique text for each player
    public TextMeshProUGUI AssignText(int conn)
    {
        // Check if the player already has an assigned text
        if (playerText.ContainsKey(conn))
        {
            return playerText[conn]; // Return the already assigned text
        }
        else if (originalTexts.Count > 0)
        {
            // Use originalTexts instead of texts to avoid permanent removal
            int textIndex = playerText.Count % originalTexts.Count;
            TextMeshProUGUI textPoint = originalTexts[textIndex];
            playerText.Add(conn, textPoint); // Assign it to the player

            Debug.Log($"Assigned text {textIndex} to player {conn}");
            return textPoint;
        }

        Debug.LogError("No available text points!");
        return null;
    }

    public override void Update()
    {
        base.Update();
        if ((SceneManager.GetActiveScene().name == "Knockout" || SceneManager.GetActiveScene().name == "Knockout 1") && oldScene != SceneManager.GetActiveScene().name)
        {
            if (SceneManager.GetActiveScene().name == "Knockout 1")
            {
                Instantiate(goal, new Vector3(-10, -24, 0), Quaternion.identity);
                Instantiate(goal1, new Vector3(-10, 24, 0), Quaternion.identity);
            }
            //Instantiate(background);
            NetworkServer.RegisterHandler<PlayerMessage>(OnCreatePlayer);
        }
        oldScene = SceneManager.GetActiveScene().name;

        // Check for auto-restart condition
        if (NetworkServer.active && enableAutoRestart)
        {
            CheckForAutoRestart();
        }
    }

    void CheckForAutoRestart()
    {
        int playerCount = NetworkServer.connections.Count;

        if (playerCount == 0)
        {
            // No players connected, start restart countdown
            if (!restartPending)
            {
                restartPending = true;
                restartCoroutine = StartCoroutine(RestartServerAfterDelay());
                Debug.Log($"No players connected. Restarting server in {restartDelay} seconds...");
            }
        }
        else
        {
            // Players are connected, cancel any pending restart
            if (restartPending)
            {
                restartPending = false;
                if (restartCoroutine != null)
                {
                    StopCoroutine(restartCoroutine);
                    restartCoroutine = null;
                }
                Debug.Log("Players reconnected. Restart cancelled.");
            }
        }
    }

    IEnumerator RestartServerAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);

        if (NetworkServer.connections.Count == 0)
        {
            Debug.Log("Restarting server - no players for " + restartDelay + " seconds");
            RestartServer();
        }

        restartPending = false;
        restartCoroutine = null;
    }

    void RestartServer()
    {
        if (!NetworkServer.active) return; // Safety check

        Debug.Log("=== SERVER RESTARTING ===");

        // Clean up existing networked objects
        CleanupNetworkedObjects();

        // Reset all game state
        ResetGameState();

        // Reset the scene if needed
        if (Application.isBatchMode)
        {
            // For dedicated servers, just reset state
            Debug.Log("Server state reset complete");
        }
        else
        {
            // For host, reload the scene
            StartCoroutine(ReloadScene());
        }
    }

    void CleanupNetworkedObjects()
    {
        Debug.Log("Cleaning up networked objects...");

        // Get all NetworkIdentities and destroy them (except NetworkManager)
        var allNetworkObjects = FindObjectsOfType<NetworkIdentity>();
        foreach (var netObj in allNetworkObjects)
        {
            // Don't destroy the NetworkManager itself
            if (netObj.gameObject != gameObject && netObj.netId != 0)
            {
                if (NetworkServer.active)
                {
                    NetworkServer.Destroy(netObj.gameObject);
                }
                else
                {
                    Destroy(netObj.gameObject);
                }
            }
        }

        Debug.Log($"Cleaned up {allNetworkObjects.Length} networked objects");
    }

    void ResetGameState()
    {
        Debug.Log("Resetting game state...");

        // Reset spawn points to original state
        spawnPoints.Clear();
        spawnPoints.AddRange(originalSpawnPoints);
        Debug.Log($"Reset {spawnPoints.Count} player spawn points");

        // Reset weapon spawn points to original state
        weaponSpawnPoints.Clear();
        weaponSpawnPoints.AddRange(originalWeaponSpawnPoints);
        Debug.Log($"Reset {weaponSpawnPoints.Count} weapon spawn points");

        // Reset texts to original state
        texts.Clear();
        texts.AddRange(originalTexts);
        Debug.Log($"Reset {texts.Count} text objects");

        // Clear all dictionaries - THIS IS CRITICAL
        playerSpawnPoints.Clear();
        playerText.Clear();
        Debug.Log("Cleared player assignments");

        // Reset all queues
        availableSpawnPoints.Clear();
        availableText.Clear();
        availableWeaponSpawnPoints.Clear();

        // Reinitialize queues with reset data
        foreach (var spawnPoint in spawnPoints)
        {
            availableSpawnPoints.Enqueue(spawnPoint);
        }

        foreach (var text in texts)
        {
            availableText.Enqueue(text);
        }

        foreach (var weaponSpawn in weaponSpawnPoints)
        {
            availableWeaponSpawnPoints.Enqueue(weaponSpawn);
        }

        // Reset other state variables
        nextSpawnIndex = 0;

        Debug.Log("Game state reset complete - ready for new players");
        Debug.Log($"Available spawn points after reset: {spawnPoints.Count}");
        Debug.Log($"Original spawn points: {originalSpawnPoints.Count}");
    }

    IEnumerator ReloadScene()
    {
        Debug.Log("Reloading scene for host...");

        // Stop the server
        StopServer();

        yield return new WaitForSeconds(1f);

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Cancel the weapon spawning routine and restart it
        CancelInvoke(nameof(TriggerSpawnRoutine));
        InvokeRepeating(nameof(TriggerSpawnRoutine), spawnInterval, spawnInterval);

        Debug.Log("Server started - ready for players");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Clean up player's assigned spawn point
        if (playerSpawnPoints.ContainsKey(conn.connectionId))
        {
            Debug.Log($"Releasing spawn point for disconnected player {conn.connectionId}");
            playerSpawnPoints.Remove(conn.connectionId);
        }

        // Clean up player's assigned text
        if (playerText.ContainsKey(conn.connectionId))
        {
            playerText.Remove(conn.connectionId);
        }

        base.OnServerDisconnect(conn);
        Debug.Log($"Player disconnected. Remaining players: {NetworkServer.connections.Count - 1}");
    }

    private void TriggerSpawnRoutine()
    {
        if (!NetworkServer.active) return; // Add safety check

        // Only spawn weapons if there are players
        if (NetworkServer.connections.Count == 0)
        {
            return;
        }

        Vector2 spawnPosition;

        if (useWeaponSpawnPoints && weaponSpawnPoints.Count > 0)
        {
            // Use predefined weapon spawn points
            spawnPosition = GetNextWeaponSpawnPoint();
        }
        else
        {
            // Fallback to random position within area
            spawnPosition = new Vector2(
                UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );
        }

        // start coroutine (will only run on the server host)
        StartCoroutine(SpawnWithIndicator(spawnPosition));
    }

    private IEnumerator SpawnWithIndicator(Vector2 position)
    {
        if (!NetworkServer.active) yield break; // Safety check

        // 1) Show the indicator
        var indicator = Instantiate(spawnIndicatorPrefab, position, Quaternion.identity);
        NetworkServer.Spawn(indicator);

        // 2) Wait for the indicatorDuration
        yield return new WaitForSeconds(indicatorDuration);

        // 3) Remove the indicator
        if (NetworkServer.active && indicator != null)
        {
            NetworkServer.Destroy(indicator);
        }

        // 4) Spawn the actual weapon
        if (NetworkServer.active && weaponPickupPrefabs != null && weaponPickupPrefabs.Length > 0)
        {
            var prefab = weaponPickupPrefabs[UnityEngine.Random.Range(0, weaponPickupPrefabs.Length)];
            var go = Instantiate(prefab, position, Quaternion.identity);
            NetworkServer.Spawn(go);
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        // Send the selected character to the server when the client connects
        int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);
        PlayerMessage message = new PlayerMessage
        {
            selectedCharacter = selectedCharacter
        };
        NetworkClient.Send(message);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Register the indicator prefab on each client so Spawn() will replicate it
        if (spawnIndicatorPrefab != null)
            NetworkClient.RegisterPrefab(spawnIndicatorPrefab);
        foreach (GameObject g in weaponPickupPrefabs)
        {
            NetworkClient.RegisterPrefab(g);
        }
    }

    void OnCreatePlayer(NetworkConnectionToClient conn, PlayerMessage message)
    {
        // Choose the prefab based on the selected character
        GameObject playerPrefab = null;

        switch (message.selectedCharacter)
        {
            default:
                playerPrefab = warriorPrefab;
                break;
        }
        // Instantiate the chosen player prefab
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        // Add the player to the server
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Get the selected character index from PlayerPrefs
        int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // Choose the prefab based on the selected character
        GameObject playerPrefab = null;

        switch (selectedCharacter)
        {
            default:
                playerPrefab = warriorPrefab;
                break;
        }
        // Instantiate the chosen player prefab at the selected spawn point
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        // Add the player to the server
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
    }
}