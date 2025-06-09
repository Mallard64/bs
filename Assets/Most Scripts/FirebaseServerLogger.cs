using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;

public class FirebaseServerLogger : MonoBehaviour
{
    [Header("Firebase Config")]
    [SerializeField] private string projectId = "smashbrawl-4fca6";
    [SerializeField] private string apiKey = "AIzaSyCXU2WmL5CfB5JBTR3_FOWJPFvYMAl-kkU";
    [SerializeField] private string databaseSecret = "XtrOS2TUuBNcKKCeKwBsHXsQL7enjeVqwF3Ree5H";

    private string serverIP = "";
    private int serverPort = 7777;
    private string databaseUrl => $"https://{projectId}-default-rtdb.firebaseio.com";

    void Start()
    {
        //if (!Application.isBatchMode) return;
        StartCoroutine(InitializeAndLog());
    }

    IEnumerator InitializeAndLog()
    {
        // Get port from Multiplay if available
        serverPort = 7777;
        var telepathy = GetComponent<TelepathyTransport>();
        if (telepathy != null) serverPort = telepathy.port;

        // Get public IP
        using (UnityWebRequest www = UnityWebRequest.Get("https://api.ipify.org"))
        {
            yield return www.SendWebRequest();
            serverIP = www.result == UnityWebRequest.Result.Success ?
                www.downloadHandler.text.Trim() :
                $"server_{System.Environment.MachineName}";
        }

        Debug.Log($"[RealtimeDB] Server: {serverIP}:{serverPort}");

        // Update every 5 seconds
        InvokeRepeating(nameof(UpdateServerStatus), 0f, 5f);

        // Set up disconnect handler
        StartCoroutine(SetupDisconnectHandler());
    }

    void UpdateServerStatus()
    {
        if (!NetworkServer.active) return;

        int playerCount = NetworkServer.connections.Count;
        StartCoroutine(SendToRealtimeDB(playerCount, true));
    }

    IEnumerator SendToRealtimeDB(int playerCount, bool online)
    {
        // Unix epoch start
        System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        long timestamp = (long)(System.DateTime.UtcNow - epoch).TotalMilliseconds;

        string json = $@"{{
            ""ip"": ""{serverIP}"",
            ""port"": {serverPort},
            ""playerCount"": {playerCount},
            ""online"": {online.ToString().ToLower()},
            ""lastUpdate"": {timestamp}
        }}";

        string serverKey = serverIP.Replace(".", "_");
        string url = $"{databaseUrl}/servers/{serverKey}.json?auth={databaseSecret}";

        using (UnityWebRequest www = UnityWebRequest.Put(url, json))
        {
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[RealtimeDB] Updated - Players: {playerCount}");
            }
            else
            {
                Debug.LogError($"[RealtimeDB] Error: {www.error}");
                Debug.LogError($"[RealtimeDB] Response: {www.downloadHandler.text}");
            }
        }
    }

    IEnumerator SetupDisconnectHandler()
    {
        string serverKey = serverIP.Replace(".", "_");
        string json = @"{""online"": false}";
        string url = $"{databaseUrl}/servers/{serverKey}/.onDisconnect.json?auth={databaseSecret}";

        using (UnityWebRequest www = UnityWebRequest.Put(url, json))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[RealtimeDB] Disconnect handler set");
            }
        }
    }

    void OnApplicationQuit()
    {
        if (Application.isBatchMode && !string.IsNullOrEmpty(serverIP))
        {
            StartCoroutine(SendToRealtimeDB(0, false));
        }
    }
}