using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;

public class RESTServerFinder : MonoBehaviour
{
    [Header("Firebase Config")]
    [SerializeField] private string projectId = "smashbrawl-4fca6";
    [SerializeField] private string databaseSecret = "XtrOS2TUuBNcKKCeKwBsHXsQL7enjeVqwF3Ree5H";

    [Header("Server Selection")]
    [Tooltip("If ≤0, skip the lastUpdate timeout check entirely.")]
    [SerializeField] private float serverTimeoutSeconds = 300f;

    public static event Action<string, int> OnServerFound;
    public static event Action<string> OnServerSearchFailed;

    private string databaseUrl => $"https://{projectId}-default-rtdb.firebaseio.com";

    public void FindAvailableServer() => StartCoroutine(FindServerCoroutine());

    private IEnumerator FindServerCoroutine()
    {
        Debug.Log("[ServerFinder] Searching for available servers…");
        string url = $"{databaseUrl}/servers.json?auth={databaseSecret}";

        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ServerFinder] Realtime DB query error: {www.error}");
            OnServerSearchFailed?.Invoke("Cannot reach server list");
            yield break;
        }

        string responseText = www.downloadHandler.text;
        if (string.IsNullOrEmpty(responseText) || responseText == "null")
        {
            Debug.Log("[ServerFinder] No servers in database");
            OnServerSearchFailed?.Invoke("No servers available");
            yield break;
        }

        var now = DateTime.UtcNow;
        var available = new List<ServerInfo>();

        // Parse each server entry from the dictionary response
        try
        {
            // Use regex to extract individual server JSON objects
            var regex = new System.Text.RegularExpressions.Regex(@"""[^""]+"":\{[^}]+\}");
            var matches = regex.Matches(responseText);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var entry = match.Value;

                // Extract just the JSON object part (everything after the first ":{")
                var startIndex = entry.IndexOf(":{") + 2;
                var jsonData = entry.Substring(startIndex, entry.Length - startIndex - 1); // Remove last }
                jsonData = "{" + jsonData + "}"; // Add braces back

                var serverData = JsonUtility.FromJson<ServerData>(jsonData);
                if (serverData == null) continue;

                // Convert timestamp from milliseconds to DateTime
                var lastUpdate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMilliseconds(serverData.lastUpdate);

                var srv = new ServerInfo
                {
                    ip = serverData.ip,
                    port = serverData.port,
                    playerCount = serverData.playerCount,
                    online = serverData.online,
                    lastUpdate = lastUpdate
                };

                var diff = (now - srv.lastUpdate).TotalSeconds;
                Debug.Log($"[ServerFinder] {srv.ip}:{srv.port} – players={srv.playerCount}, diff={diff:F1}s");

                if (IsServerAvailable(srv, now, diff))
                {
                    available.Add(srv);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerFinder] JSON parsing failed: {ex}");
            Debug.LogError($"[ServerFinder] Raw response: {responseText}");
            OnServerSearchFailed?.Invoke("Failed to parse server data");
            yield break;
        }

        // Custom sorting: prioritize servers with 1 player, then 0 players
        available.Sort((a, b) =>
        {
            // Priority 1: Servers with 1 player
            if (a.playerCount == 1 && b.playerCount != 1) return -1;
            if (b.playerCount == 1 && a.playerCount != 1) return 1;

            // Priority 2: Servers with 0 players
            if (a.playerCount == 0 && b.playerCount != 0) return -1;
            if (b.playerCount == 0 && a.playerCount != 0) return 1;

            // If same priority level, sort by player count ascending
            return a.playerCount.CompareTo(b.playerCount);
        });

        if (available.Count > 0)
        {
            var best = available[0];
            Debug.Log($"[ServerFinder] Chosen {best.ip}:{best.port} with {best.playerCount} players");
            OnServerFound?.Invoke(best.ip, best.port);
        }
        else
        {
            Debug.Log("[ServerFinder] No suitable servers found");
            OnServerSearchFailed?.Invoke("All servers are full or stale");
        }
    }

    private bool IsServerAvailable(ServerInfo srv, DateTime now, double ageSeconds)
    {
        if (string.IsNullOrEmpty(srv.ip)) return false;
        if (!srv.online) return false;
        if (srv.playerCount >= 2) return false; // Don't join servers with 2+ players

        // only apply timeout if >0
        if (serverTimeoutSeconds > 0f && ageSeconds > serverTimeoutSeconds)
        {
            Debug.Log($"[ServerFinder] {srv.ip} timed out after {ageSeconds:F1}s");
            return false;
        }
        return true;
    }

    // ─── JSON Data Classes ───

    [Serializable]
    private class ServerData
    {
        public string ip;
        public int port;
        public int playerCount;
        public bool online;
        public long lastUpdate; // Unix timestamp in milliseconds
    }

    [Serializable]
    private class ServerInfo
    {
        public string ip;
        public int port;
        public int playerCount;
        public bool online;
        public DateTime lastUpdate;
    }
}