using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    [Header("Scene Names")]
    public string winScene = "Win";
    public string tieScene = "Tie";
    public string loseScene = "Lose";

    [Header("Rewards")]
    public int coinsPerKill = 10;

    [Header("UI Slots")]
    public TextMeshProUGUI leftText;   // assign in inspector: the left‐side UI
    public TextMeshProUGUI rightText;  // assign in inspector: the right‐side UI
    public TextMeshProUGUI timerText;  // assign in inspector: timer display UI

    [SyncVar(hook = nameof(OnP1ScoreChanged))]
    private int player1Kills;

    [SyncVar(hook = nameof(OnP2ScoreChanged))]
    private int player2Kills;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float gameTimer = 0f;

    [SyncVar(hook = nameof(OnGameStateChanged))]
    private bool gameActive = false;

    private const float GAME_DURATION = 120f; // 120 seconds

    // Filled in on each client once their local Enemy spawns:
    private int localPlayerNum = 0;
    private bool localPlayerFound = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        // initialize scores and timer
        player1Kills = 0;
        player2Kills = 0;
        gameTimer = 0f;
        gameActive = false;
    }

    public void Reset()
    {
        player1Kills = 0;
        player2Kills = 0;
        gameTimer = 0f;
        gameActive = false;
    }



    public override void OnStartClient()
    {
        base.OnStartClient();
        // Only try to find local player if we're not a dedicated server
        if (!NetworkServer.active || NetworkManager.singleton.mode == NetworkManagerMode.Host)
        {
            TryFindLocalPlayer();
        }
    }

    void Update()
    {
        // Only try to find local player on clients (not dedicated server)
        if (isClient && (!NetworkServer.active || NetworkManager.singleton.mode == NetworkManagerMode.Host))
        {
            TryFindLocalPlayer();
        }

        // Only server handles timer logic
        if (isServer)
        {
            UpdateGameState();
        }
    }

    [Server]
    void HandleGameEnd()
    {
        // On dedicated server, just disconnect clients and stop server
        if (NetworkServer.active && NetworkManager.singleton.mode == NetworkManagerMode.ServerOnly)
        {
            // Disconnect all clients
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn != null)
                {
                    conn.Disconnect();
                }
            }
        }
        else
        {
            // Host behavior - disconnect clients first
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn != null)
                {
                    conn.Disconnect();
                }
            }

            // Stop the host
            NetworkManager.singleton.StopHost();

            // Determine outcome for this host instance and load appropriate scene
            DetermineAndLoadScene();
        }
    }

    void DetermineAndLoadScene()
    {
        // Only run on clients (not dedicated server)
        if (NetworkManager.singleton.mode == NetworkManagerMode.ServerOnly) return;

        if (!localPlayerFound) return;

        int localKills = (localPlayerNum == 1) ? player1Kills : player2Kills;
        int otherKills = (localPlayerNum == 1) ? player2Kills : player1Kills;

        string sceneToLoad;
        if (localKills > otherKills)
        {
            sceneToLoad = winScene;
        }
        else if (localKills == otherKills)
        {
            sceneToLoad = tieScene;
        }
        else
        {
            sceneToLoad = loseScene;
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    // This will be called on clients when they get disconnected
    public override void OnStopClient()
    {
        base.OnStopClient();
        // Only handle scene loading on actual clients (not dedicated server)
        if (NetworkManager.singleton.mode != NetworkManagerMode.ServerOnly)
        {
            // Small delay to ensure final score updates are received
            Invoke(nameof(DetermineAndLoadScene), 0.1f);
        }
    }

    [Server]
    void UpdateGameState()
    {
        int playerCount = FindObjectsOfType<Enemy>().Length;

        if (playerCount == 2)
        {
            if (!gameActive)
            {
                // Start the game
                gameActive = true;
                gameTimer = GAME_DURATION;
            }
            else if (gameTimer > 0)
            {
                // Update timer
                gameTimer -= Time.deltaTime;
                if (gameTimer <= 0)
                {
                    gameTimer = 0;
                    // Game ended - handle end game
                    HandleGameEnd();
                }
            }
        }
        else
        {
            // Reset everything if not 2 players
            if (gameActive || player1Kills > 0 || player2Kills > 0 || gameTimer > 0)
            {
                gameActive = false;
                player1Kills = 0;
                player2Kills = 0;
                gameTimer = 0f;
            }
        }
    }

    void TryFindLocalPlayer()
    {
        // Skip if we're on a dedicated server
        if (NetworkManager.singleton.mode == NetworkManagerMode.ServerOnly) return;

        foreach (var e in FindObjectsOfType<Enemy>())
        {
            if (e.isLocalPlayer)
            {
                localPlayerNum = e.playerNum;  // needs public or internal access
                localPlayerFound = true;
                UpdateTexts();
                break;
            }
        }
    }

    [Server]
    public void AddKill(int playerId)
    {
        // Only add kills if game is active
        if (!gameActive) return;

        if (playerId == 1)
        {
            player1Kills++;
            // Award coins to player 1 on their client
            RpcAwardCoins(playerId);
        }
        else if (playerId == 2)
        {
            player2Kills++;
            // Award coins to player 2 on their client
            RpcAwardCoins(playerId);
        }
    }

    [ClientRpc]
    void RpcAwardCoins(int playerId)
    {
        // Skip on dedicated server
        if (NetworkManager.singleton.mode == NetworkManagerMode.ServerOnly) return;

        // Only award coins to the local player who got the kill
        if (localPlayerFound && localPlayerNum == playerId)
        {
            ShopMenu shopMenu = FindObjectOfType<ShopMenu>();
            if (shopMenu != null)
            {
                shopMenu.AddCoins(coinsPerKill);
            }
        }
    }

    // Hooks fire on **all clients** when the SyncVar changes:
    void OnP1ScoreChanged(int _, int newVal)
    {
        player1Kills = newVal;
        UpdateTexts();
    }

    void OnP2ScoreChanged(int _, int newVal)
    {
        player2Kills = newVal;
        UpdateTexts();
    }

    void OnTimerChanged(float _, float newVal)
    {
        gameTimer = newVal;
        UpdateTexts();
    }

    void OnGameStateChanged(bool _, bool newVal)
    {
        gameActive = newVal;
        UpdateTexts();
    }

    // Puts local player's kills on the leftText, other player's on rightText
    void UpdateTexts()
    {
        // Skip UI updates on dedicated server
        if (NetworkManager.singleton.mode == NetworkManagerMode.ServerOnly) return;

        if (!localPlayerFound) return;

        int localKills = (localPlayerNum == 1) ? player1Kills : player2Kills;
        int otherKills = (localPlayerNum == 1) ? player2Kills : player1Kills;

        if (leftText != null) leftText.text = localKills.ToString();
        if (rightText != null) rightText.text = otherKills.ToString();

        // Update timer display
        if (timerText != null)
        {
            if (gameActive)
            {
                int minutes = Mathf.FloorToInt(gameTimer / 60f);
                int seconds = Mathf.FloorToInt(gameTimer % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            else
            {
                timerText.text = "Waiting for players...";
            }
        }
    }

    // Public getter for other scripts that might need game state
    public bool IsGameActive() => gameActive;
    public float GetTimeRemaining() => gameTimer;
}