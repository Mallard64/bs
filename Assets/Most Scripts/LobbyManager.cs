using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    [Header("Lobby Settings")]
    public Transform[] playerSpawnPoints;
    public int maxPlayersInLobby = 8;
    
    [Header("Leaderboard")]
    public GameObject leaderboardPanel;
    public Transform leaderboardContent;
    public GameObject leaderboardEntryPrefab;
    
    [Header("Portals")]
    public Portal bossPortal;
    public Portal knockoutPortal;
    
    [Header("Fun Features")]
    public WeaponDisplay weaponDisplay;
    public MusicPlayer musicPlayer;
    public ParticleSystem ambientEffects;
    
    [SyncVar]
    public int currentPlayersInLobby = 0;
    
    private static LobbyManager _instance;
    public static LobbyManager Instance => _instance;
    
    // Player statistics for leaderboard
    [System.Serializable]
    public class PlayerStats
    {
        public string playerName;
        public int totalKills;
        public int gamesWon;
        public int gamesPlayed;
        public float winRate => gamesPlayed > 0 ? (float)gamesWon / gamesPlayed * 100f : 0f;
        public int highScore;
        public long playTime; // in seconds
    }
    
    private Dictionary<uint, PlayerStats> playerStatistics = new Dictionary<uint, PlayerStats>();
    
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
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        LoadPlayerStatistics();
        SetupLobbyFeatures();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            SetupClientUI();
        }
    }
    
    void SetupLobbyFeatures()
    {
        // Initialize ambient effects
        if (ambientEffects != null)
        {
            ambientEffects.Play();
        }
        
        // Setup music
        if (musicPlayer != null)
        {
            musicPlayer.PlayLobbyMusic();
        }
        
        // Setup weapon display rotation
        if (weaponDisplay != null)
        {
            weaponDisplay.StartRotation();
        }
        
        Debug.Log("🏛️ Lobby features initialized!");
    }
    
    void SetupClientUI()
    {
        StartCoroutine(RefreshLeaderboardPeriodically());
    }
    
    IEnumerator RefreshLeaderboardPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // Refresh every 10 seconds
            CmdRequestLeaderboardUpdate();
        }
    }
    
    [Command(requiresAuthority = false)]
    void CmdRequestLeaderboardUpdate()
    {
        RpcUpdateLeaderboard();
    }
    
    [ClientRpc]
    void RpcUpdateLeaderboard()
    {
        UpdateLeaderboardDisplay();
    }
    
    void UpdateLeaderboardDisplay()
    {
        if (leaderboardContent == null || leaderboardEntryPrefab == null) return;
        
        // Clear existing entries
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
        
        // Sort players by win rate, then by total kills
        var sortedStats = new List<PlayerStats>(playerStatistics.Values);
        sortedStats.Sort((a, b) => {
            int winRateCompare = b.winRate.CompareTo(a.winRate);
            if (winRateCompare == 0)
                return b.totalKills.CompareTo(a.totalKills);
            return winRateCompare;
        });
        
        // Create leaderboard entries
        for (int i = 0; i < Mathf.Min(sortedStats.Count, 10); i++)
        {
            var stats = sortedStats[i];
            var entry = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            var leaderboardEntry = entry.GetComponent<LeaderboardEntry>();
            
            if (leaderboardEntry != null)
            {
                leaderboardEntry.SetupEntry(i + 1, stats);
            }
        }
        
        Debug.Log($"📊 Updated leaderboard with {sortedStats.Count} players");
    }
    
    public void UpdatePlayerStats(uint netId, string playerName, int kills, bool wonGame)
    {
        if (!playerStatistics.ContainsKey(netId))
        {
            playerStatistics[netId] = new PlayerStats { playerName = playerName };
        }
        
        var stats = playerStatistics[netId];
        stats.totalKills += kills;
        stats.gamesPlayed++;
        if (wonGame) stats.gamesWon++;
        
        SavePlayerStatistics();
    }
    
    void LoadPlayerStatistics()
    {
        // In a real implementation, load from PlayerPrefs or a database
        // For now, create some sample data
        playerStatistics.Clear();
        
        // Sample leaderboard data
        playerStatistics[1001] = new PlayerStats 
        { 
            playerName = "SkillMaster", 
            totalKills = 156, 
            gamesWon = 23, 
            gamesPlayed = 30,
            highScore = 4500,
            playTime = 7200
        };
        
        playerStatistics[1002] = new PlayerStats 
        { 
            playerName = "QuickShot", 
            totalKills = 98, 
            gamesWon = 18, 
            gamesPlayed = 25,
            highScore = 3200,
            playTime = 5400
        };
        
        playerStatistics[1003] = new PlayerStats 
        { 
            playerName = "Warrior_X", 
            totalKills = 203, 
            gamesWon = 31, 
            gamesPlayed = 45,
            highScore = 5100,
            playTime = 9800
        };
    }
    
    void SavePlayerStatistics()
    {
        // In a real implementation, save to PlayerPrefs or a database
        Debug.Log("💾 Player statistics saved");
    }
    
    [Server]
    public Transform GetAvailableSpawnPoint()
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            return transform;
            
        // Find a spawn point that's not too close to other players
        foreach (var spawnPoint in playerSpawnPoints)
        {
            bool isAvailable = true;
            Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(spawnPoint.position, 2f);
            
            foreach (var obj in nearbyObjects)
            {
                // Check for actual players (with MouseShooting component)
                var mouseShooting = obj.GetComponent<MouseShooting>();
                if (mouseShooting != null)
                {
                    isAvailable = false;
                    break;
                }
            }
            
            if (isAvailable)
                return spawnPoint;
        }
        
        // If all spawn points are occupied, return a random one
        return playerSpawnPoints[Random.Range(0, playerSpawnPoints.Length)];
    }
    
    public void ToggleLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            bool isActive = !leaderboardPanel.activeSelf;
            leaderboardPanel.SetActive(isActive);
            
            if (isActive)
            {
                UpdateLeaderboardDisplay();
            }
        }
    }
}