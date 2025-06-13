using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

[System.Serializable]
public class PlayerData
{
    public int level = 1;
    public int experience = 0;
    public int coins = 100;
    public List<string> unlockedSkins = new List<string> { "default" };
    public Dictionary<string, int> stats = new Dictionary<string, int>();
    public string lastSave = "";

    public PlayerData()
    {
        stats["gamesPlayed"] = 0;
        stats["wins"] = 0;
        stats["highScore"] = 0;
    }
}

public class FirebaseRESTSaveManager : MonoBehaviour
{
    private static FirebaseRESTSaveManager instance;
    public static FirebaseRESTSaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FirebaseRESTSaveManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("FirebaseRESTSaveManager");
                    instance = go.AddComponent<FirebaseRESTSaveManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("Firebase Configuration")]
    [SerializeField] private string firebaseProjectId = "your-project-id";
    [SerializeField] private string firebaseWebApiKey = "your-web-api-key";

    [Header("Current Session")]
    [SerializeField] private string currentUserId = "";
    [SerializeField] private string currentIdToken = "";

    [Header("Player Data")]
    public PlayerData currentPlayerData = new PlayerData();

    [Header("Settings")]
    public bool autoSave = true;
    public float autoSaveInterval = 30f;

    private float lastSaveTime;
    private string databaseUrl => $"https://{firebaseProjectId}-default-rtdb.firebaseio.com";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (autoSave && !string.IsNullOrEmpty(currentUserId) && Time.time - lastSaveTime > autoSaveInterval)
        {
            SaveData();
            lastSaveTime = Time.time;
        }
    }

    // Call this after successful login
    public void SetUserCredentials(string userId, string idToken)
    {
        currentUserId = userId;
        currentIdToken = idToken;
        Debug.Log($"[SaveManager REST] User credentials set for: {userId}");

        // Load user data after setting credentials
        LoadData();
    }

    public void SaveData()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("[SaveManager REST] Cannot save - no user logged in");
            return;
        }

        StartCoroutine(SaveDataCoroutine());
    }

    private IEnumerator SaveDataCoroutine()
    {
        currentPlayerData.lastSave = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Build JSON manually since Unity's JsonUtility doesn't handle dictionaries
        StringBuilder json = new StringBuilder();
        json.Append("{");
        json.Append($"\"level\":{currentPlayerData.level},");
        json.Append($"\"experience\":{currentPlayerData.experience},");
        json.Append($"\"coins\":{currentPlayerData.coins},");
        json.Append($"\"lastSave\":\"{currentPlayerData.lastSave}\",");

        // Serialize unlocked skins array
        json.Append("\"unlockedSkins\":[");
        for (int i = 0; i < currentPlayerData.unlockedSkins.Count; i++)
        {
            json.Append($"\"{currentPlayerData.unlockedSkins[i]}\"");
            if (i < currentPlayerData.unlockedSkins.Count - 1) json.Append(",");
        }
        json.Append("],");

        // Serialize stats dictionary
        json.Append("\"stats\":{");
        int statCount = 0;
        foreach (var stat in currentPlayerData.stats)
        {
            json.Append($"\"{stat.Key}\":{stat.Value}");
            if (++statCount < currentPlayerData.stats.Count) json.Append(",");
        }
        json.Append("}");

        json.Append("}");

        string jsonData = json.ToString();

        Debug.Log($"[SaveManager REST] Saving data for user: {currentUserId}");
        Debug.Log($"[SaveManager REST] JSON being sent: {jsonData}");
        Debug.Log($"[SaveManager REST] Level: {currentPlayerData.level}, Coins: {currentPlayerData.coins}, XP: {currentPlayerData.experience}");
        Debug.Log($"[SaveManager REST] Unlocked Skins: {string.Join(", ", currentPlayerData.unlockedSkins)}");

        // Create REST request
        string url = $"{databaseUrl}/users/{currentUserId}.json?auth={currentIdToken}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[SaveManager REST] Data saved successfully!");
                Debug.Log($"[SaveManager REST] Response: {request.downloadHandler.text}");
                SaveLocalBackup();
            }
            else
            {
                Debug.LogError($"[SaveManager REST] Failed to save: {request.error}");
                Debug.LogError($"[SaveManager REST] Response: {request.downloadHandler.text}");
            }
        }
    }

    public void LoadData()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("[SaveManager REST] Cannot load - no user logged in");
            LoadLocalBackup();
            return;
        }

        StartCoroutine(LoadDataCoroutine());
    }

    private IEnumerator LoadDataCoroutine()
    {
        Debug.Log($"[SaveManager REST] Loading data for user: {currentUserId}");

        string url = $"{databaseUrl}/users/{currentUserId}.json?auth={currentIdToken}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (responseText != "null" && !string.IsNullOrEmpty(responseText))
                {
                    Debug.Log("[SaveManager REST] Found existing player data");
                    Debug.Log($"[SaveManager REST] Raw data: {responseText}");

                    try
                    {
                        // Parse JSON manually since Unity's JsonUtility is limited
                        var data = SimpleJSON.Parse(responseText);

                        if (data != null)
                        {
                            currentPlayerData.level = data["level"].AsInt;
                            currentPlayerData.experience = data["experience"].AsInt;
                            currentPlayerData.coins = data["coins"].AsInt;
                            currentPlayerData.lastSave = data["lastSave"].Value;

                            // Load skins
                            if (data["unlockedSkins"] != null)
                            {
                                currentPlayerData.unlockedSkins.Clear();
                                foreach (var skin in data["unlockedSkins"].AsArray)
                                {
                                    currentPlayerData.unlockedSkins.Add(skin.Value);
                                }
                            }

                            // Load stats
                            if (data["stats"] != null)
                            {
                                currentPlayerData.stats.Clear();
                                foreach (var stat in data["stats"].AsObject)
                                {
                                    currentPlayerData.stats[stat.Key] = stat.Value.AsInt;
                                }
                            }

                            Debug.Log($"[SaveManager REST] Loaded - Level: {currentPlayerData.level}, Coins: {currentPlayerData.coins}, XP: {currentPlayerData.experience}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveManager REST] Failed to parse data: {e.Message}");
                        LoadLocalBackup();
                    }
                }
                else
                {
                    Debug.Log("[SaveManager REST] No existing data found - creating new player data");
                    currentPlayerData = new PlayerData();
                    SaveData();
                }

                SaveLocalBackup();
            }
            else
            {
                Debug.LogError($"[SaveManager REST] Failed to load: {request.error}");
                LoadLocalBackup();
            }
        }
    }

    private void SaveLocalBackup()
    {
        PlayerPrefs.SetInt("Local_Level", currentPlayerData.level);
        PlayerPrefs.SetInt("Local_Experience", currentPlayerData.experience);
        PlayerPrefs.SetInt("Local_Coins", currentPlayerData.coins);
        PlayerPrefs.SetString("Local_Skins", string.Join(",", currentPlayerData.unlockedSkins));
        PlayerPrefs.SetString("Local_LastSave", currentPlayerData.lastSave);

        foreach (var stat in currentPlayerData.stats)
        {
            PlayerPrefs.SetInt($"Local_Stat_{stat.Key}", stat.Value);
        }

        PlayerPrefs.Save();
        Debug.Log("[SaveManager REST] Local backup saved");
    }

    private void LoadLocalBackup()
    {
        if (PlayerPrefs.HasKey("Local_Level"))
        {
            Debug.Log("[SaveManager REST] Loading from local backup");

            currentPlayerData.level = PlayerPrefs.GetInt("Local_Level", 1);
            currentPlayerData.experience = PlayerPrefs.GetInt("Local_Experience", 0);
            currentPlayerData.coins = PlayerPrefs.GetInt("Local_Coins", 100);
            currentPlayerData.lastSave = PlayerPrefs.GetString("Local_LastSave", "");

            string skinsString = PlayerPrefs.GetString("Local_Skins", "default");
            currentPlayerData.unlockedSkins = new List<string>(skinsString.Split(','));

            currentPlayerData.stats["gamesPlayed"] = PlayerPrefs.GetInt("Local_Stat_gamesPlayed", 0);
            currentPlayerData.stats["wins"] = PlayerPrefs.GetInt("Local_Stat_wins", 0);
            currentPlayerData.stats["highScore"] = PlayerPrefs.GetInt("Local_Stat_highScore", 0);

            Debug.Log($"[SaveManager REST] Loaded local backup - Level: {currentPlayerData.level}, Coins: {currentPlayerData.coins}");
        }
    }

    // Game methods remain the same
    public void AddCoins(int amount)
    {
        currentPlayerData.coins += amount;
        Debug.Log($"[SaveManager REST] Added {amount} coins. Total: {currentPlayerData.coins}");
    }

    public bool SpendCoins(int amount)
    {
        if (currentPlayerData.coins >= amount)
        {
            currentPlayerData.coins -= amount;
            Debug.Log($"[SaveManager REST] Spent {amount} coins. Remaining: {currentPlayerData.coins}");
            return true;
        }
        return false;
    }

    public void AddExperience(int amount)
    {
        currentPlayerData.experience += amount;
        Debug.Log($"[SaveManager REST] Added {amount} XP. Total: {currentPlayerData.experience}");

        int newLevel = (currentPlayerData.experience / 100) + 1;
        if (newLevel > currentPlayerData.level)
        {
            currentPlayerData.level = newLevel;
            Debug.Log($"[SaveManager REST] LEVEL UP! New level: {currentPlayerData.level}");
        }
    }

    public void UnlockSkin(string skinId)
    {
        if (!currentPlayerData.unlockedSkins.Contains(skinId))
        {
            currentPlayerData.unlockedSkins.Add(skinId);
            Debug.Log($"[SaveManager REST] Unlocked new skin: {skinId}");
        }
    }

    public bool HasSkin(string skinId)
    {
        return currentPlayerData.unlockedSkins.Contains(skinId);
    }

    public void IncrementStat(string statName, int amount = 1)
    {
        if (!currentPlayerData.stats.ContainsKey(statName))
        {
            currentPlayerData.stats[statName] = 0;
        }
        currentPlayerData.stats[statName] += amount;
    }

    public void UpdateHighScore(int score)
    {
        if (score > currentPlayerData.stats["highScore"])
        {
            currentPlayerData.stats["highScore"] = score;
        }
    }

    [ContextMenu("Debug - Print Current Data")]
    void DebugPrintData()
    {
        Debug.Log("=== CURRENT PLAYER DATA (REST) ===");
        Debug.Log($"User ID: {currentUserId}");
        Debug.Log($"Level: {currentPlayerData.level}");
        Debug.Log($"Experience: {currentPlayerData.experience}");
        Debug.Log($"Coins: {currentPlayerData.coins}");
        Debug.Log($"Unlocked Skins: {string.Join(", ", currentPlayerData.unlockedSkins)}");
        Debug.Log("Stats:");
        foreach (var stat in currentPlayerData.stats)
        {
            Debug.Log($"  {stat.Key}: {stat.Value}");
        }
        Debug.Log($"Last Save: {currentPlayerData.lastSave}");
        Debug.Log("==================================");
    }

    [ContextMenu("Force Save")]
    public void ForceSave() => SaveData();

    [ContextMenu("Force Load")]
    public void ForceLoad() => LoadData();

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && !string.IsNullOrEmpty(currentUserId))
        {
            SaveData();
        }
    }
}



// Extension helper removed - no longer needed